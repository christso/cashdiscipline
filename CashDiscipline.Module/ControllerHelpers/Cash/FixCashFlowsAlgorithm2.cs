﻿using CashDiscipline.Module.BusinessObjects.Forex;
using CashDiscipline.Module.BusinessObjects.Setup;
using CashDiscipline.Module.ParamObjects.Cash;
using Xafology.ExpressApp.Xpo;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xafology.Spreadsheet.Attributes;
using DevExpress.Persistent.BaseImpl;
using Xafology.ExpressApp.Xpo.Import;
using CashDiscipline.Module.BusinessObjects.Cash;

namespace CashDiscipline.Module.ControllerHelpers.Cash
{
    public class FixCashFlowsAlgorithm2 : IFixCashFlows
    {
        private XPObjectSpace objSpace;
        private CashFlowFixParam paramObj;
        private Activity paramApReclassActivity;
        private Counterparty defaultCounterparty;
        private CashForecastFixTag reversalFixTag;
        private CashForecastFixTag revRecFixTag;
        private CashForecastFixTag resRevRecFixTag;
        private CashForecastFixTag payrollFixTag;

        private List<CashFlow> cashFlowsToDelete;

        public FixCashFlowsAlgorithm2(XPObjectSpace objSpace, CashFlowFixParam paramObj)
        {
            this.objSpace = objSpace;
            this.paramObj = paramObj;
            // default values and parameters
            if (paramObj.ApReclassActivity == null)
                throw new InvalidOperationException("AP Reclass Activity must be defined.");
            paramApReclassActivity = objSpace.GetObjectByKey<Activity>(objSpace.GetKeyValue(paramObj.ApReclassActivity));
            defaultCounterparty = objSpace.FindObject<Counterparty>(
             CriteriaOperator.Parse("Name LIKE ?", "UNDEFINED"));
            reversalFixTag = objSpace.FindObject<CashForecastFixTag>(
                CriteriaOperator.Parse("Name = 'R'"));
            revRecFixTag = objSpace.FindObject<CashForecastFixTag>(
                CriteriaOperator.Parse("Name = 'RR'"));
            resRevRecFixTag = objSpace.FindObject<CashForecastFixTag>(
                CriteriaOperator.Parse("Name = 'RRR'"));
            payrollFixTag = objSpace.FindObject<CashForecastFixTag>(
                CriteriaOperator.Parse("Name = 'PY'"));

            this.cashFlowsToDelete = new List<CashFlow>();
        }

        #region Reset

        // delete existing fixes
        public void Reset()
        {
            DeleteOrphans();
            ResetFixStatus();
        }

        // delete fixes that are not linked to any cash flow
        // because they have been unlinked
        public void DeleteOrphans()
        {
            CashFlowSnapshot currentSnapshot = GetCurrentSnapshot(objSpace.Session);

            var cashFlowFixes = new XPQuery<CashFlow>(objSpace.Session)
                .Where(cf =>
                cf.Snapshot.Oid == currentSnapshot.Oid
                && cf.Fix != null
                && (cf.Fix == reversalFixTag
                    || cf.Fix == revRecFixTag
                    || cf.Fix == resRevRecFixTag)
                && cf.ParentCashFlow == null);

            foreach (var cashFlowFix in cashFlowFixes)
                cashFlowFix.Delete();

            objSpace.CommitChanges();
        }

        public void ResetFixStatus()
        {
            CashFlowSnapshot currentSnapshot = GetCurrentSnapshot(objSpace.Session);

            var cashFlows = new XPQuery<CashFlow>(objSpace.Session)
                .Where(cf =>
                cf.Snapshot.Oid == currentSnapshot.Oid);
            foreach (var cashFlow in cashFlows)
            {
                cashFlow.IsFixeeSynced = false;
                cashFlow.IsFixerSynced = false;
            }
            objSpace.CommitChanges();
        }
        #endregion

        #region Process

        public void ProcessCashFlows()
        {
            cashFlowsToDelete.Clear();

            DeleteOrphans();

            var cashFlows = GetCashFlowsToFix().OrderBy(x => x.TranDate);

            foreach (var cashFlow in cashFlows)
            {
                ProcessCashFlowsFromFixer(cashFlows, cashFlow);
                cashFlow.IsFixerSynced = true;
                cashFlow.IsFixerFixeesSynced = true;
            }

            objSpace.Delete(cashFlowsToDelete);
            cashFlowsToDelete.Clear();

            objSpace.CommitChanges();
        }

        private void ProcessCashFlowsFromFixer(IEnumerable<CashFlow> cashFlows, CashFlow fixer)
        {
            // process from fixer. Note that the number of Fixees will reduce during the process.
            var fixees = GetFixees(cashFlows, fixer);
            foreach (var fixee in fixees)
            {
                foreach (var child in fixee.ChildCashFlows)
                {
                    cashFlowsToDelete.Add(child);
                }
                CreateFixes(fixer, fixee);
                fixee.IsFixeeSynced = true;
            }
        }

        public void CreateFixes(CashFlow fixer, CashFlow fixee)
        {
            var rev = objSpace.CreateObject<CashFlow>();
            rev.TranDate = fixer.TranDate;
            rev.AccountCcyAmt = -fixee.AccountCcyAmt;
            rev.Fix = reversalFixTag;
            rev.ParentCashFlow = fixee;
        }

        // This will return all cash flows which have changed after it was fixed
        public IEnumerable<CashFlow> GetCashFlowsToFix()
        {
            CashFlowSnapshot currentSnapshot = GetCurrentSnapshot(objSpace.Session);
            var cashFlows = new XPQuery<CashFlow>(objSpace.Session)
                .Where(cf =>
                cf.TranDate >= paramObj.FromDate && cf.TranDate <= paramObj.ToDate
                && cf.Snapshot.Oid == currentSnapshot.Oid
                && (cf.Fix == null || cf.Fix.FixTagType != CashForecastFixTagType.Ignore)
                && (!cf.IsFixeeSynced && !cf.IsFixerSynced) || !cf.IsFixerFixeesSynced);

            return cashFlows;
        }

        public IEnumerable<CashFlow> GetFixees(IEnumerable<CashFlow> cashFlows, CashFlow fixer)
        {
            // we add "fixee.IsFixeeSynced == false"
            // since one fixee can have many fixers, we avoid
            // running the algorithm twice on the same fixee
            return cashFlows.Where((fixee) => GetFixCriteria(fixee, fixer) && !fixee.IsFixeeSynced);
        }

        public bool GetFixCriteria(CashFlow fixee, CashFlow fixer)
        {
            return fixee.DateUnFix >= fixer.FixFromDate && fixee.DateUnFix <= fixer.FixToDate
                        // should we do activity = fixactivity as well?
                        && fixee.FixActivity == fixer.FixActivity

                        && fixer.Status == CashFlowStatus.Forecast
                        && fixer.FixRank > fixee.FixRank
                        && (fixer.Counterparty == null || fixer.Counterparty == defaultCounterparty
                        || fixee.Counterparty == null && fixer.Counterparty == null
                        || fixee.Counterparty != null && fixer.Counterparty == fixee.Counterparty.FixCounterparty
                        )
                        && fixee.Account != null && fixee.Account.FixAccount == fixer.Account.FixAccount;
        }

        #endregion

        #region Helpers

        private CashFlowSnapshot GetCurrentSnapshot(Session session)
        {
            return CashFlowHelper.GetCurrentSnapshot(session);
        }

        #endregion

    }
}

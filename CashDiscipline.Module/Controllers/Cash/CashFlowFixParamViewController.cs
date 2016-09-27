﻿using CashDiscipline.Module.BusinessObjects.Cash;
using CashDiscipline.Module.Logic.Cash;
using CashDiscipline.Module.ParamObjects.Cash;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CashDiscipline.Module.Controllers.Cash
{
    public class CashFlowFixParamViewController : ViewController
    {
        public CashFlowFixParamViewController()
        {
            TargetObjectType = typeof(CashFlowFixParam);
            
            var runAction = new SimpleAction(this, "CashFlowRunAction", "ExecuteActions");
            runAction.Caption = "Run";
            runAction.Execute += (sender, e) => RunAction_Execute(sender, e);

            var fixAction = new SimpleAction(this, "CashFlowFixAction", "ExecuteActions");
            fixAction.Caption = "Fix";
            fixAction.Execute += (sender, e) => FixAction_Execute(sender, e, true);

            var unfixAction = new SimpleAction(this, "CashFlowUnfixAction", "ExecuteActions");
            unfixAction.Caption = "Unfix";
            unfixAction.Execute += (sender, e) => UnfixAction_Execute(sender, e, true);

            var mapAction = new SimpleAction(this, "CashFlowMapAction", "ExecuteActions");
            mapAction.Caption = "Map";
            mapAction.Execute += (sender, e) => MapAction_Execute(sender, e, true);

            var revalAction = new SimpleAction(this, "CashFlowRevalAction", "ExecuteActions");
            revalAction.Caption = "Reval";
            revalAction.Execute += (sender, e) => RevalAction_Execute(sender, e, true);

        }

        private void RunAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            MapAction_Execute(sender, e, false);
            FixAction_Execute(sender, e, false);
            RevalAction_Execute(sender, e, false);
            new Xafology.ExpressApp.SystemModule.GenericMessageBox(
                "Cash Flows were successfully 'Mapped', 'Fixed' and 'Revalued'.",
                "Cash Flow Fix SUCCESS");
        }

        private void RevalAction_Execute(object sender, SimpleActionExecuteEventArgs e, bool isRootSender = false)
        {
            var os = (XPObjectSpace)Application.CreateObjectSpace();
            var paramObj = View.CurrentObject as CashFlowFixParam;
            var revaluer = new RevalueAccounts(os, paramObj);
            revaluer.Process();

            if (isRootSender)
            {
                new Xafology.ExpressApp.SystemModule.GenericMessageBox(
                    "Foreign Currency Account balances were successfully 'Revalued'.",
                    "Cash Balance Revaluation SUCCESS");
            }
        }

        private void MapAction_Execute(object sender, SimpleActionExecuteEventArgs e, bool isRootSender = false)
        {
            var os = (XPObjectSpace)Application.CreateObjectSpace();
            var paramObj = View.CurrentObject as CashFlowFixParam;
            if (paramObj != null)
            {
                var algo = new FixCashFlowsAlgorithm(os, paramObj);
                algo.Map();
            }
            if (isRootSender)
            {
                new Xafology.ExpressApp.SystemModule.GenericMessageBox(
                    "Cash Flows were successfully 'Mapped'.",
                    "Cash Flow Map SUCCESS");
            }
        }

        private void UnfixAction_Execute(object sender, SimpleActionExecuteEventArgs e, bool isRootSender = false)
        {
            var os = (XPObjectSpace)Application.CreateObjectSpace();
            var paramObj = View.CurrentObject as CashFlowFixParam;
            if (paramObj != null)
            {
                var algo = new FixCashFlowsAlgorithm(os, paramObj);
                algo.Reset();
            }

            if (isRootSender)
            {
                new Xafology.ExpressApp.SystemModule.GenericMessageBox(
                    "Cash Flows were successfully 'Unfixed'.",
                    "Cash Flow Unfix SUCCESS");
            }
        }

        private void FixAction_Execute(object sender, SimpleActionExecuteEventArgs e, bool isRootSender = false)
        {
            ObjectSpace.CommitChanges();

            var os = (XPObjectSpace)Application.CreateObjectSpace();
            var paramObj = View.CurrentObject as CashFlowFixParam;
            if (paramObj != null)
            {
                var algo = new FixCashFlowsAlgorithm(os, paramObj);
                algo.ProcessCashFlows();
            }

            if (isRootSender)
            {
                new Xafology.ExpressApp.SystemModule.GenericMessageBox(
                    "Cash Flows were successfully 'Fixed'.",
                    "Cash Flow Fix SUCCESS");
            }
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            var detailView = View as DetailView;
            if (detailView != null)
                detailView.ViewEditMode = ViewEditMode.Edit;
        }
    }
}

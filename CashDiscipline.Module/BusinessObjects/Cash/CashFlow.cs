using CashDiscipline.Module.BusinessObjects.Forex;
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
using DevExpress.Persistent.BaseImpl;
using Xafology.ExpressApp.Xpo.Import;
using CashDiscipline.Module.Logic.Cash;
using Xafology.ExpressApp.Xpo.SequentialGuidBase;
using CashDiscipline.Module.Attributes;
using Xafology.ExpressApp.BatchDelete;
using CashDiscipline.Module.BusinessObjects.AccountsPayable;
using CashDiscipline.Module.Interfaces;

// Please note that calling Save() will set IsFixeeSynced and IsFixerSynced to false;
// Therefore, avoid calling Save() if you need to update those properties.
namespace CashDiscipline.Module.BusinessObjects.Cash
{
    [VisibleInReports(true)]
    [ModelDefault("IsCloneable", "True")]
    [ModelDefault("IsFooterVisible", "True")]
    [ModelDefault("ImageName","BO_List")]
    [DefaultListViewOptions(allowEdit: true, newItemRowPosition: NewItemRowPosition.Top)]
    [DefaultProperty("Oid")]
    [AutoColumnWidth(false)]
    [BatchDelete(isVisible: true)]
    public class CashFlow : SequentialGuidBaseObject, ICalculateToggleObject, ICashFlow, 
        IXpoImportable
    {

        private Activity _FixActivity;
        private DateTime _FixToDate;
        private DateTime _FixFromDate;
        private DateTime _DateUnFix;
        private bool _IsReclass;
        private CashForecastFixTag _Fix;
        private int _FixRank;
        private Account _Account;
        private CashFlowStatus _Status;
        private Counterparty _Counterparty;
        private Currency _CounterCcy;
        private decimal _CounterCcyAmt;
        private decimal _FunctionalCcyAmt;
        private decimal _AccountCcyAmt;
        private string _Description;
        private Activity _Activity;
        private DateTime _TranDate;
        private CashFlowForexSettleType _ForexSettleType;
        private CashFlow origCashFlow;
        private long _ForexSettleGroupId;
        private bool calculateEnabled;
        private DateTime timeEntered;

        public CashFlow()
        {

        }

        public CashFlow(Session session)
            : this(session, true)
        {

        }

        public CashFlow(Session session, bool calculateEnabled)
            : base(session)
        {
            this.calculateEnabled = calculateEnabled;
            this.Changed += CashFlow_Changed;
        }

        // TODO: move logic to view controller
        private void CashFlow_Changed(object sender, ObjectChangeEventArgs e)
        {
            if (!this.IsLoading)
            {
                
            }
        }

        // Whether Real Time calculation is enabled. If not, then calculation will occur on saving.
        [MemberDesignTimeVisibility(false), Browsable(false), NonPersistent]
        public bool CalculateEnabled
        {
            get { return calculateEnabled; }
            set
            {
                calculateEnabled = value;
            }
        }

        #region Main Properties

        public CashFlow OrigCashFlow
        {
            get
            {
                return origCashFlow;
            }
            set
            {
                SetPropertyValue("OrigCashFlow", ref origCashFlow, value);
            }
        }

        private CashFlowSnapshot _Snapshot;
        [Association("CashFlowSnapshot-CashFlows")]
        [ModelDefault("AllowEdit", "false")]
        public CashFlowSnapshot Snapshot
        {
            get
            {
                return _Snapshot;
            }
            set
            {
                SetPropertyValue("Snapshot", ref _Snapshot, value);
            }
        }

        public CashFlowStatus Status
        {
            get
            {
                return _Status;
            }
            set
            {
                SetPropertyValue("Status", ref _Status, value);
            }
        }

        [ModelDefault("EditMask", "dd-MMM-yy")]
        [ModelDefault("DisplayFormat", "dd-MMM-yy")]
        [RuleRequiredField("CashFlow.TranDate_RuleRequiredField", DefaultContexts.Save)]
        public DateTime TranDate
        {
            get
            {
                return _TranDate;
            }
            set
            {
                SetPropertyValue("TranDate", ref _TranDate, value.Date);
            }
        }

        [MemberDesignTimeVisibility(false)]
        [DevExpress.Xpo.DisplayName("Account")]
        public string AccountName
        {
            get
            {
                return Account.Name;
            }
        }

        [RuleRequiredField("CashFlow.Account_RuleRequiredField", DefaultContexts.Save)]
        [ImmediatePostData(true)]
        public Account Account
        {
            get
            {
                return _Account;
            }
            set
            {
                if (SetPropertyValue("Account", ref _Account, value))
                {
                    if (calculateEnabled && !IsLoading && !IsSaving && value != null)
                    {
                        SetPropertyValue("CounterCcy", ref _CounterCcy, value.Currency);
                    }
                }
            }
        }

        [MemberDesignTimeVisibility(false)]
        [DevExpress.Xpo.DisplayName("Activity")]
        public string ActivityName
        {
            get
            {
                return Activity.Name;
            }
        }

        //[Association("Activity-CashFlows")]
        [RuleRequiredField("CashFlow.Activity_RuleRequiredField", DefaultContexts.Save)]
        public Activity Activity
        {
            get
            {
                return _Activity;
            }
            set
            {
                SetPropertyValue("Activity", ref _Activity, value);
            }
        }

        [RuleRequiredField("CashFlow.Counterparty_RuleRequiredField", DefaultContexts.Save)]
        public Counterparty Counterparty
        {
            get
            {
                return _Counterparty;
            }
            set
            {
                SetPropertyValue("Counterparty", ref _Counterparty, value);
            }
        }

        [EditorAlias("Xafology_DecimalActionPropertyEditor")]
        [ModelDefault("EditMask", "n2")]
        [ModelDefault("DisplayFormat", "n2")]
        [ImmediatePostData(true)]
        public decimal AccountCcyAmt
        {
            get
            {
                return _AccountCcyAmt;
            }
            set
            {
                if (SetPropertyValue("AccountCcyAmt", ref _AccountCcyAmt, Math.Round(value, 2)))
                {
                    if (!IsLoading && !IsSaving && Account != null && CalculateEnabled
                        && TranDate != default(DateTime))
                    {
                        UpdateFunctionalCcyAmt(this, value, Account.Currency);
                        UpdateCounterCcyAmt(this, value, Account.Currency);
                    }
                }
            }
        }

        [EditorAlias("Xafology_DecimalActionPropertyEditor")]
        [ModelDefault("EditMask", "n2")]
        [ModelDefault("DisplayFormat", "n2")]
        public decimal FunctionalCcyAmt
        {
            get
            {
                return _FunctionalCcyAmt;
            }
            set
            {
                if (SetPropertyValue("FunctionalCcyAmt", ref _FunctionalCcyAmt, Math.Round(value, 2)))
                {
                    if (!IsSaving && !IsLoading)
                    {
                        UpdateBankStmtsFunctionalCcyAmt();
                    }
                }
            }
        }

        [EditorAlias("Xafology_DecimalActionPropertyEditor")]
        [ModelDefault("EditMask", "n2")]
        [ModelDefault("DisplayFormat", "n2")]
        [ImmediatePostData(true)]
        public decimal CounterCcyAmt
        {
            get
            {
                return _CounterCcyAmt;
            }
            set
            {
                if (SetPropertyValue("CounterCcyAmt", ref _CounterCcyAmt, Math.Round(value, 2)))
                {
                    if (!IsLoading && CounterCcy != null && CalculateEnabled && TranDate != default(DateTime))
                    {
                        UpdateFunctionalCcyAmt(this, CounterCcyAmt, CounterCcy);
                        UpdateAccountCcyAmt(this, CounterCcyAmt, CounterCcy);
                    }
                }
            }
        }

        [MemberDesignTimeVisibility(false)]
        [DevExpress.Xpo.DisplayName("Counter Ccy")]
        public string CounterCcyName
        {
            get
            {
                return CounterCcy.Name;
            }
        }

        [Association("Currency-CashFlows")]
        [RuleRequiredField("CashFlow.CounterCcy_RuleRequiredField", DefaultContexts.Save)]
        [ImmediatePostData(true)]
        public Currency CounterCcy
        {
            get
            {
                return _CounterCcy;
            }
            set
            {
                if (SetPropertyValue("CounterCcy", ref _CounterCcy, value))
                {
                    if (!IsLoading && CounterCcy != null && CalculateEnabled && TranDate != default(DateTime))
                    {
                        UpdateFunctionalCcyAmt(this, CounterCcyAmt, CounterCcy);
                        UpdateAccountCcyAmt(this, CounterCcyAmt, CounterCcy);
                    }
                }
            }
        }

        [Size(SizeAttribute.Unlimited)]
        public string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                SetPropertyValue("Description", ref _Description, value);
            }
        }

        private CashFlowSource _Source;
        [Association("CashFlowSource-CashFlows")]
        public CashFlowSource Source
        {
            get
            {
                return _Source;
            }
            set
            {
                SetPropertyValue("Source", ref _Source, value);
            }
        }

        [ModelDefault("DisplayFormat", "dd-MMM-yy hh:mm:ss tt")]
        public DateTime TimeEntered
        {
            get
            {
                return timeEntered;
            }
            set
            {
                SetPropertyValue("TimeEntered", ref timeEntered, value);
            }
        }

        [PersistentAlias("Activity.ActivityL1")]
        public string ActivityL1
        {
            get { return Convert.ToString(EvaluateAlias("ActivityL1")); }
        }

        [PersistentAlias("Activity.ActivityL2")]
        public string ActivityL2
        {
            get { return Convert.ToString(EvaluateAlias("ActivityL2")); }
        }

        [PersistentAlias("Activity.ActivityL3")]
        public string ActivityL3
        {
            get { return Convert.ToString(EvaluateAlias("ActivityL3")); }
        }

        [PersistentAlias("Activity.ActivityL4")]
        public string ActivityL4
        {
            get { return Activity.ActivityL4; }
        }

        [Association("CashFlow-BankStmts")]
        public XPCollection<BankStmt> BankStmts
        {
            get
            {
                return GetCollection<BankStmt>("BankStmts");
            }
        }


        [Association("CashFlow-ApPmtDistns")]
        public XPCollection<ApPmtDistn> ApPmtDistns
        {
            get
            {
                return GetCollection<ApPmtDistn>("ApPmtDistns");
            }
        }

        [Association("PrimaryCashFlow-ForexTrades")]
        public XPCollection<ForexTrade> PrimaryCashFlowForexTrades
        {
            get
            {
                return GetCollection<ForexTrade>("PrimaryCashFlowForexTrades");
            }
        }

        [Association("CounterCashFlow-ForexTrades")]
        public XPCollection<ForexTrade> CounterCashFlowForexTrades
        {
            get
            {
                return GetCollection<ForexTrade>("CounterCashFlowForexTrades");
            }
        }

        #endregion

        #region Amount Calculators

        public void CalculateAmounts()
        {
            UpdateFunctionalCcyAmt();
            UpdateAccountCcyAmt();
            UpdateCounterCcyAmt();
        }

        public void UpdateAccountCcyAmt()
        {
            if (CounterCcyAmt != 0 && CounterCcy != null && TranDate != default(DateTime))
            {
                UpdateAccountCcyAmt(this, CounterCcyAmt, CounterCcy);
            }
        }

        public static void UpdateAccountCcyAmt(CashFlow obj, decimal fromAmt, Currency fromCcy)
        {
            if (obj.Account == null) return;
            var session = obj.Session;
            if (obj.Account.Currency == null)
                throw new UserFriendlyException("No currency for account " + obj.Account.Name);
            if (obj.Account.Currency.Oid == fromCcy.Oid)
                obj.AccountCcyAmt = fromAmt;
            else if (obj.TranDate != default(DateTime))
            {
                var rateObj = GetForexRateObject(session, fromCcy, obj.Account.Currency, (DateTime)obj.TranDate);
                if (rateObj != null)
                {
                    // TODO: do not assume that Functional Currency is 'AUD'
                    var value = fromAmt * (decimal)rateObj.ConversionRate;
                    obj.SetPropertyValue("AccountCcyAmt", ref obj._AccountCcyAmt, value);
                }
            }
        }

        public void UpdateFunctionalCcyAmt()
        {
            UpdateFunctionalCcyAmt(this, CounterCcyAmt, CounterCcy);
            if (CounterCcyAmt != 0 && CounterCcy != null && TranDate != default(DateTime))
            {
                UpdateFunctionalCcyAmt(this, CounterCcyAmt, CounterCcy);
            }
            else if (AccountCcyAmt != 0 && Account != null && Account.Currency != null
                & TranDate != default(DateTime))
            {
                UpdateFunctionalCcyAmt(this, AccountCcyAmt, Account.Currency);
            }
        }

        public void UpdateFunctionalCcyAmt(decimal fromAmt, Currency fromCcy)
        {
            CashFlow.UpdateFunctionalCcyAmt(this, fromAmt, fromCcy);
        }

        public static void UpdateFunctionalCcyAmt(CashFlow obj, decimal fromAmt, Currency fromCcy)
        {
            if (obj == null || fromCcy == null) return;
            var session = obj.Session;

            var funcCcy = SetOfBooks.GetInstance(session).FunctionalCurrency;

            if (SetOfBooks.CachedInstance.FunctionalCurrency.Oid == fromCcy.Oid)
                obj.FunctionalCcyAmt = fromAmt;
            else if (obj.TranDate != default(DateTime))
            {
                var rateObj = GetForexRateObject(session, fromCcy, funcCcy, (DateTime)obj.TranDate);
                if (rateObj != null)
                {
                    var value = fromAmt * (decimal)rateObj.ConversionRate;
                    obj.FunctionalCcyAmt = value;
                }
            }
        }

        public void UpdateCounterCcyAmt()
        {
            if (AccountCcyAmt != 0 && Account != null && Account.Currency != null
                & TranDate != default(DateTime))
            {
                UpdateCounterCcyAmt(this, AccountCcyAmt, Account.Currency);
            }
        }

        public static void UpdateCounterCcyAmt(CashFlow obj, decimal fromAmt, Currency fromCcy)
        {
            if (obj.CounterCcy == null) return;
            var session = obj.Session;
            if (obj.CounterCcy.Oid == fromCcy.Oid)
                obj.CounterCcyAmt = fromAmt;
            else if (obj.TranDate != default(DateTime))
            {
                var rateObj = GetForexRateObject(session, fromCcy, obj.CounterCcy, (DateTime)obj.TranDate);
                if (rateObj != null)
                {
                    // TODO: do not assume that Local Currency is 'AUD'
                    var value = Math.Round(fromAmt * (decimal)rateObj.ConversionRate, 2);
                    obj.SetPropertyValue("CounterCcyAmt", ref obj._CounterCcyAmt, value);
                }
            }
        }

        #endregion

        #region Fixing

        public int FixRank
        {
            get
            {
                return _FixRank;
            }
            set
            {
                SetPropertyValue("FixRank", ref _FixRank, value);
            }
        }
        
        public CashForecastFixTag Fix
        {
            get
            {
                return _Fix;
            }
            set
            {
                SetPropertyValue("Fix", ref _Fix, value);
            }
        }

        public bool IsReclass
        {
            get
            {
                return _IsReclass;
            }
            set
            {
                SetPropertyValue("IsReclass", ref _IsReclass, value);
            }
        }

        [ModelDefault("DisplayFormat", "dd-MMM-yy")]
        [ModelDefault("EditMask", "dd-MMM-yy")]
        public DateTime DateUnFix
        {
            get
            {
                return _DateUnFix;
            }
            set
            {
                SetPropertyValue("DateUnFix", ref _DateUnFix, value);
            }
        }

        [ModelDefault("DisplayFormat", "dd-MMM-yy")]
        [ModelDefault("EditMask", "dd-MMM-yy")]
        public DateTime FixFromDate
        {
            get
            {
                return _FixFromDate;
            }
            set
            {
                SetPropertyValue("FixFromDate", ref _FixFromDate, value);
            }
        }

        [ModelDefault("DisplayFormat", "dd-MMM-yy")]
        [ModelDefault("EditMask", "dd-MMM-yy")]
        public DateTime FixToDate
        {
            get
            {
                return _FixToDate;
            }
            set
            {
                SetPropertyValue("FixToDate", ref _FixToDate, value);
            }
        }


        public Activity FixActivity
        {
            get
            {
                return _FixActivity;
            }
            set
            {
                SetPropertyValue("FixActivity", ref _FixActivity, value);
            }
        }

        private CashFlow _Fixer;
        [ModelDefault("LookupProperty", "Oid")]
        [Association("CashFlowFixer-CashFlows")]
        public CashFlow Fixer
        {
            get
            {
                return _Fixer;
            }
            set
            {
                SetPropertyValue("Fixer", ref _Fixer, value);
            }
        }

        [Association("ParentCashFlow-ChildCashFlows")]
        public XPCollection<CashFlow> ChildCashFlows
        {
            get
            {
                return GetCollection<CashFlow>("ChildCashFlows");
            }
        }

        private CashFlow _ParentCashFlow;
        [Association("ParentCashFlow-ChildCashFlows")]
        public CashFlow ParentCashFlow
        {
            get
            {
                return _ParentCashFlow;
            }
            set
            {
                SetPropertyValue("ParentCashFlow", ref _ParentCashFlow, value);
            }
        }

        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public DateTime ParentTranDate
        {
            get
            {
                if (ParentCashFlow == null)
                    return TranDate;
                return ParentCashFlow.TranDate;
            }
        }

        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public Activity ParentActivity
        {
            get
            {
                if (ParentCashFlow == null)
                    return Activity;
                return ParentCashFlow.ParentActivity;
            }
        }

        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public CashFlowSource ParentSource
        {
            get
            {
                if (ParentCashFlow == null)
                    return Source;
                return ParentCashFlow.Source;
            }
        }

        [Association("CashFlowFixer-CashFlows")]
        public XPCollection<CashFlow> Fixees
        {
            get
            {
                return GetCollection<CashFlow>("Fixees");
            }
        }

        #endregion

        #region Forex
        public CashFlowForexSettleType ForexSettleType
        {
            get
            {
                return _ForexSettleType;
            }
            set
            {
                SetPropertyValue("ForexSettleType", ref _ForexSettleType, value);
            }
        }

        [Association("CashFlowIn-ForexSettleLinks")]
        public XPCollection<ForexSettleLink> ForexSettleLinksIn
        {
            get
            {
                return GetCollection<ForexSettleLink>("ForexSettleLinksIn");
            }
        }

        [Association("CashFlowOut-ForexSettleLinks")]
        public XPCollection<ForexSettleLink> ForexSettleLinksOut
        {
            get
            {
                return GetCollection<ForexSettleLink>("ForexSettleLinksOut");
            }
        }

        private static ForexRate GetForexRateObject(Session session, Currency fromCcy, Currency toCcy, DateTime convDate)
        {
            return ForexRate.GetForexRateObject(fromCcy, toCcy, convDate);
        }

        private static decimal GetForexRate(Session session, Currency fromCcy, Currency toCcy, DateTime convDate)
        {
            return ForexRate.GetForexRate(fromCcy, toCcy, convDate);
        }

        #endregion

        #region Forex Trade Calculators
        
        public long ForexSettleGroupId
        {
            get
            {
                return _ForexSettleGroupId;
            }
            set
            {
                SetPropertyValue("ForexSettleGroupId", ref _ForexSettleGroupId, value);
            }
        }
        #endregion

        #region Forex Calculators

        private bool IsForexAccount
        {
            get
            {
                if (SetOfBooks.CachedInstance == null || this.Account == null) return false;
                if (this.Account == null) return false;
                if (this.Account.Currency == null) return false;
                if (SetOfBooks.CachedInstance.FunctionalCurrency.Oid != this.Account.Currency.Oid) return true;
                return false;
            }
        }
        
        private void UpdateBankStmtsFunctionalCcyAmt()
        {
            // why is BankStmts.Count == 0?
            foreach (BankStmt detail in BankStmts)
            {
                detail.FunctionalCcyAmt = detail.TranAmount * FunctionalCcyAmt / AccountCcyAmt;
            }
        }

        #endregion


        #region Class Methods

        public override void AfterConstruction()
        {
            base.AfterConstruction();
            calculateEnabled = true;
            InitDefaultValues();
        }

        public void InitDefaultValues()
        {
            Snapshot = GetCurrentSnapshot(Session);

            TranDate = DateTime.Now.Date;

            CounterCcy = Session.GetObjectByKey<Currency>(SetOfBooks.CachedInstance.FunctionalCurrency.Oid);

            var defObj = CashFlowDefaults.GetInstance(Session);
            if (defObj == null) return;

            Counterparty = defObj.Counterparty;
            Account = defObj.Account;
            Activity = defObj.Activity;
        }

        protected override void OnSaving()
        {
            TimeEntered = DateTime.Now;

            base.OnSaving();
        }

        protected override void OnSaved()
        {
            base.OnSaved();
        }

        protected override void OnDeleting()
        {
            //while (Fixees.Count != 0)
            //    Fixees[0].Fixer = null;

            //ParentCashFlow = null;

            //while (ChildCashFlows.Count != 0)
            //{
            //    ChildCashFlows[0].Delete();
            //}

            base.OnDeleting();
        }

        protected override void OnLoaded()
        {
            Reset();
            base.OnLoaded();
        }

        protected void Reset()
        {
            ForexSettleLinksIn.Reload();
            ForexSettleLinksOut.Reload();
        }

        #endregion

        #region Helpers

        public static DateTime GetMaxActualTranDate(Session session)
        {
            return CashFlowHelper.GetMaxActualTranDate(session);
        }

        #endregion

        #region Fix Cash Flow Algorithm

        public static void FixCashFlows(XPObjectSpace objSpace, CashFlowFixParam paramObj)
        {
            var algo = new FixCashFlowsAlgorithm(objSpace, paramObj);
            algo.ProcessCashFlows();
        }

        #endregion

        #region Snapshot

        private static CashFlowSnapshot GetCurrentSnapshot(Session session)
        {
            return session.GetObjectByKey<CashFlowSnapshot>(SetOfBooks.CachedInstance.CurrentCashFlowSnapshot.Oid);
        }

        #endregion

        #region Field Operators

        public new class Fields
        {
            public static OperandProperty SnapshotOid
            {
                get
                {
                    return new OperandProperty("Snapshot." + CashFlowSnapshot.Fields.Oid.PropertyName);
                }
            }
            public static OperandProperty TranDate
            {
                get
                {
                    return new OperandProperty("TranDate");
                }
            }

            public static OperandProperty Snapshot
            {
                get
                {
                    return new OperandProperty("Snapshot");
                }
            }

            public static OperandProperty Source
            {
                get
                {
                    return new OperandProperty("Source");
                }
            }
            public static OperandProperty AccountCcyAmt
            {
                get
                {
                    return new OperandProperty("AccountCcyAmt");
                }
            }
            public static OperandProperty FunctionalCcyAmt
            {
                get
                {
                    return new OperandProperty("FunctionalCcyAmt");
                }
            }
            public static OperandProperty CounterCcyAmt
            {
                get
                {
                    return new OperandProperty("CounterCcyAmt");
                }
            }
            public static OperandProperty Status
            {
                get
                {
                    return new OperandProperty("Status");
                }
            }

            public static OperandProperty IsFixeeSynced
            {
                get { return new OperandProperty("IsFixeeSynced"); }
            }

            public static OperandProperty IsFixerSynced
            {
                get { return new OperandProperty("IsFixerSynced"); }
            }

            public static OperandProperty IsFixerFixeesSynced
            {
                get { return new OperandProperty("IsFixerFixeesSynced"); }
            }

            public static OperandProperty IsFixSynced
            {
                get { return new OperandProperty("IsFixSynced"); }
            }


            public static OperandProperty TimeEntered
            {
                get { return new OperandProperty("TimeEntered"); }
            }
        }
        #endregion

    }
    //used by DailyCashUpdate - @ActualStatus
    public enum CashFlowStatus
    {
        Forecast = 0,
        Actual = 1
    }
}

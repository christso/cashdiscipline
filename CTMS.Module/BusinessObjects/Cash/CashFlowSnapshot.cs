using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using System;
using System.ComponentModel;

namespace CTMS.Module.BusinessObjects.Cash
{
    [DefaultProperty("Name")]
    [ImageName("BO_List")]
    [ModelDefault("IsCloneable", "True")]
    [NavigationItem("Cash Setup")]
    [DefaultListViewOptions(allowEdit: true, newItemRowPosition: NewItemRowPosition.Top)]
    [ModelDefault("IsFooterVisible", "True")]
    public class CashFlowSnapshot : BaseObject
    { // Inherit from a different class to provide a custom primary key, concurrency and deletion behavior, etc. (http://documentation.devexpress.com/#Xaf/CustomDocument3146).
        public CashFlowSnapshot(Session session)
            : base(session)
        {
        }
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            TimeCreated = DateTime.Now;
        }

        private DateTime _TimeCreated;
        private DateTime _FromDate;

        private string _Name;

        public DateTime TimeCreated
        {
            get
            {
                return _TimeCreated;
            }
            set
            {
                SetPropertyValue("TimeCreated", ref _TimeCreated, value);
            }
        }

        [RuleUniqueValue("CashFlowSnapshot_Name_RuleUniqueValue", DefaultContexts.Save)]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                SetPropertyValue("Name", ref _Name, value);
            }
        }

        private int _SequentialNumber;
        [RuleUniqueValue("CashFlowSnapshot_SequentialNumber_RuleUniqueValue", DefaultContexts.Save)]
        public int SequentialNumber
        {
            get
            {
                return _SequentialNumber;
            }
            set
            {
                SetPropertyValue("Name", ref _SequentialNumber, value);
            }
        }

        public DateTime FromDate
        {
            get
            {
                return _FromDate;
            }
            set
            {
                SetPropertyValue("FromDate", ref _FromDate, value);
            }
        }

        [Association("CashFlowSnapshot-CashFlows"), DevExpress.Xpo.Aggregated]
        public XPCollection<CashFlow> CashFlows
        {
            get
            {
                return GetCollection<CashFlow>("CashFlows");
            }
        }

        protected override void OnDeleting()
        {
            base.OnDeleting();
        }

        public new class Fields
        {
            public static OperandProperty Oid { get { return new OperandProperty("Oid"); } }
            public static OperandProperty Name { get { return new OperandProperty("Name"); } }
        }
        public class FieldNames
        {
            public static string Oid { get { return Fields.Oid.PropertyName; } }
            public static string Name { get { return Fields.Name.PropertyName; } }
        }
    }
}

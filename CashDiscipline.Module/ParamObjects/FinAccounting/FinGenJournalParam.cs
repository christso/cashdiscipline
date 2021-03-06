using System;
using System.Linq;
using System.Text;
using DevExpress.Xpo;
using DevExpress.ExpressApp;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;
using System.Collections.Generic;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Xpo;

namespace CashDiscipline.Module.ParamObjects.FinAccounting
{
    [ModelDefault("ImageName", "BO_List")]
    [DefaultProperty("Oid")]
    //[RuleCriteria("FinGenJournalParam_CannotDeleteSingleton", DefaultContexts.Delete, "False",
    //CustomMessageTemplate = "Cannot delete Singleton.")]
    public class FinGenJournalParam : BaseObject
    {
        public FinGenJournalParam(Session session)
            : base(session)
        {
            session.LockingOption = LockingOption.None;
        }
        public override void AfterConstruction()
        {
            base.AfterConstruction();
        }

        public static FinGenJournalParam GetInstance(IObjectSpace objectSpace)
        {
            FinGenJournalParam result = objectSpace.FindObject<FinGenJournalParam>(null);
            if (result == null)
            {
                result = new FinGenJournalParam(((XPObjectSpace)objectSpace).Session);
                result.Save(); // this will also sync the child objects
            }
            return result;
        }

        private DateTime _FromDate;
        private DateTime _ToDate;

        [ModelDefault("DisplayFormat", "dd-MMM-yy")]
        [ModelDefault("EditMask", "dd-MMM-yy")]
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

        [ModelDefault("DisplayFormat", "dd-MMM-yy")]
        [ModelDefault("EditMask", "dd-MMM-yy")]
        public DateTime ToDate
        {
            get
            {
                return _ToDate;
            }
            set
            {
                SetPropertyValue("ToDate", ref _ToDate, value);
            }
        }

        [Association("FinGenJournalParam-FinJournalGroupParams"), DevExpress.Xpo.Aggregated]
        public XPCollection<FinJournalGroupParam> JournalGroupParams
        {
            get
            {
                return GetCollection<FinJournalGroupParam>("JournalGroupParams");
            }
        }
    }
}

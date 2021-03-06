﻿using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using System;
using System.ComponentModel;

namespace CashDiscipline.Module.BusinessObjects.Cash
{
    [ImageName("BO_List")]
    [ModelDefault("IsCloneable", "True")]
    [NavigationItem("Cash Data")]
    [DefaultListViewOptions(allowEdit: true, newItemRowPosition: NewItemRowPosition.Top)]
    [ModelDefault("IsFooterVisible", "True")]
    public class CashSnapshotReported : BaseObject
    {
        public CashSnapshotReported(Session session)
            : base(session)
        {
        }

        public override void AfterConstruction()
        {
            base.AfterConstruction();
            IsEnabled = true;
        }

        private string _CompareName;
        [RuleUniqueValue("CashFlowSnapshot_CompareName_RuleUniqueValue", DefaultContexts.Save)]
        public string CompareName
        {
            get
            {
                return _CompareName;
            }
            set
            {
                SetPropertyValue("CompareName", ref _CompareName, value);
            }
        }

        private CashFlowSnapshot _CurrentSnapshot;

        public CashFlowSnapshot CurrentSnapshot
        {
            get
            {
                return _CurrentSnapshot;
            }
            set
            {
                SetPropertyValue("Snapshot", ref _CurrentSnapshot, value);
            }
        }

        private CashFlowSnapshot _PreviousSnapshot;

        public CashFlowSnapshot PreviousSnapshot
        {
            get
            {
                return _PreviousSnapshot;
            }
            set
            {
                SetPropertyValue("Snapshot", ref _PreviousSnapshot, value);
            }
        }

        private bool _IsEnabled;
        public bool IsEnabled
        {
            get
            {
                return _IsEnabled;
            }
            set
            {
                SetPropertyValue("IsEnabled", ref _IsEnabled, value);
            }
        }

    }

}

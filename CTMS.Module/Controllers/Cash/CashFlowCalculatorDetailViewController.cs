﻿using CTMS.Module.BusinessObjects.Cash;
using Xafology.ExpressApp.Editors;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;

namespace CTMS.Module.Controllers.Cash
{
    public class CashFlowCalculatorDetailViewController : ViewController<DetailView>
    {
        public CashFlowCalculatorDetailViewController()
        {
            TargetObjectType = typeof(CashFlow);
        }

        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();
            var viewItems = View.GetItems<IActionPropertyEditor>();
            foreach (var viewItem in viewItems)
            {
                var editor = ((PropertyEditor)viewItem);
                if (editor.Id == CashFlow.FieldNames.AccountCcyAmt)
                    viewItem.ButtonClick += accountCcyAmtCalculator;
                else if (editor.Id == CashFlow.FieldNames.CounterCcyAmt)
                    viewItem.ButtonClick += counterCcyAmtCalculator;
                else if (editor.Id == CashFlow.FieldNames.FunctionalCcyAmt)
                    viewItem.ButtonClick += functionalCcyAmtCalculator;
            }
        }

        #region Action Property Calculators

        private void accountCcyAmtCalculator(PropertyEditor sender, ActionPropertyClickEventArgs e)
        {
            var obj = (CashFlow)View.CurrentObject;
            obj.UpdateAccountCcyAmt();
            sender.Refresh();
        }

        private void functionalCcyAmtCalculator(PropertyEditor sender, ActionPropertyClickEventArgs e)
        {
            var obj = (CashFlow)View.CurrentObject;
            obj.UpdateFunctionalCcyAmt();
            sender.Refresh();
        }

        private void counterCcyAmtCalculator(PropertyEditor sender, ActionPropertyClickEventArgs e)
        {
            var obj = (CashFlow)View.CurrentObject;
            obj.UpdateCounterCcyAmt();
            sender.Refresh();
        }
        #endregion
    }
}

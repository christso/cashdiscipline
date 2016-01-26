﻿using CTMS.Module.BusinessObjects.Cash;
using CTMS.Module.BusinessObjects.Cash.AccountsPayable;

using CTMS.Module.ParamObjects.Import;
using DevExpress.ExpressApp;
using d2import = Xafology.ExpressApp.Xpo.Import;

namespace CTMS.Module.Controllers
{
    public class ImportListViewController : ViewController
    {
        protected void ImportDataActionExecute(object sender, DevExpress.ExpressApp.Actions.SingleChoiceActionExecuteEventArgs e)
        {
            if (e.SelectedChoiceActionItem.Caption == "Default")
                DefaultImportDataActionExecute();

        }

        #region Default Import
        private void DefaultImportDataActionExecute()
        {
            if (View.ObjectTypeInfo.Type == typeof(CTMS.Module.BusinessObjects.Forex.ForexRate))
            {
                var paramObj = new ImportForexRatesParam();
                ShowParamView(paramObj);
            }
            else if (View.ObjectTypeInfo.Type == typeof(BankStmt))
            {
                var paramObj = new ImportBankStmtParam();
                ShowParamView(paramObj);
            }
            else if (View.ObjectTypeInfo.Type == typeof(ApPmtDistn))
            {
                var paramObj = new ImportApPmtDistnParam();
                ShowParamView(paramObj);
            }
        }

        private void ShowParamView(object paramObj)
        {
            //show view
        }
        #endregion
    }
}

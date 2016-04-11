namespace CashDiscipline.Win
{
    partial class CashDisciplineWindowsFormsApplication
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.module1 = new DevExpress.ExpressApp.SystemModule.SystemModule();
            this.module2 = new DevExpress.ExpressApp.Win.SystemModule.SystemWindowsFormsModule();
            this.module3 = new CashDiscipline.Module.CashDisciplineModule();
            this.module4 = new CashDiscipline.Module.Win.CashDisciplineWindowsFormsModule();
            this.sqlConnection1 = new System.Data.SqlClient.SqlConnection();
            this.businessClassLibraryCustomizationModule1 = new DevExpress.ExpressApp.Objects.BusinessClassLibraryCustomizationModule();
            this.cloneObjectModule1 = new DevExpress.ExpressApp.CloneObject.CloneObjectModule();
            this.viewVariantsModule1 = new DevExpress.ExpressApp.ViewVariantsModule.ViewVariantsModule();
            this.validationModule1 = new DevExpress.ExpressApp.Validation.ValidationModule();
            this.pivotGridModule1 = new DevExpress.ExpressApp.PivotGrid.PivotGridModule();
            this.reportsModule1 = new DevExpress.ExpressApp.Reports.ReportsModule();
            this.scriptRecorderModuleBase1 = new DevExpress.ExpressApp.ScriptRecorder.ScriptRecorderModuleBase();
            this.scriptRecorderWindowsFormsModule1 = new DevExpress.ExpressApp.ScriptRecorder.Win.ScriptRecorderWindowsFormsModule();
            this.reportsWindowsFormsModule1 = new DevExpress.ExpressApp.Reports.Win.ReportsWindowsFormsModule();
            this.pivotGridWindowsFormsModule1 = new DevExpress.ExpressApp.PivotGrid.Win.PivotGridWindowsFormsModule();
            this.pivotChartModuleBase1 = new DevExpress.ExpressApp.PivotChart.PivotChartModuleBase();
            this.pivotChartWindowsFormsModule1 = new DevExpress.ExpressApp.PivotChart.Win.PivotChartWindowsFormsModule();
            this.CashDisciplinePivotGridWinModule1 = new Xafology.ExpressApp.PivotGrid.Win.XafologyPivotGridWinModule();
            this.XafologySystemModule1 = new Xafology.ExpressApp.SystemModule.XafologySystemModule();
            this.fileAttachmentsWindowsFormsModule1 = new DevExpress.ExpressApp.FileAttachments.Win.FileAttachmentsWindowsFormsModule();
            this.pivotGridLayoutModule1 = new Xafology.ExpressApp.PivotGridLayout.PivotGridLayoutModule();
            this.xpoModule1 = new Xafology.ExpressApp.Xpo.XpoModule();
            this.pivotGridLayoutWindowsFormsModule1 = new Xafology.ExpressApp.PivotGridLayout.Win.PivotGridLayoutWindowsFormsModule();
            this.layoutModule1 = new Xafology.ExpressApp.Layout.LayoutModule();
            this.layoutWindowsFormsModule1 = new Xafology.ExpressApp.Layout.Win.LayoutWindowsFormsModule();
            this.XafologySystemWindowsFormsModule1 = new Xafology.ExpressApp.Win.SystemModule.XafologySystemWindowsFormsModule();
            this.msoExcelModule1 = new Xafology.ExpressApp.MsoExcel.MsoExcelModule();
            this.importModule1 = new Xafology.ExpressApp.Xpo.Import.ImportModule();
            this.batchDeleteWinModule1 = new Xafology.ExpressApp.BatchDelete.Win.BatchDeleteWinModule();
            this.pasteWinModule1 = new Xafology.ExpressApp.Paste.Win.PasteWinModule();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // sqlConnection1
            // 
            this.sqlConnection1.ConnectionString = "Integrated Security=SSPI;Pooling=false;Data Source=.\\SQLEXPRESS;Initial Catalog=C" +
    "TMS";
            this.sqlConnection1.FireInfoMessageEventOnUserErrors = false;
            // 
            // validationModule1
            // 
            this.validationModule1.AllowValidationDetailsAccess = true;
            this.validationModule1.IgnoreWarningAndInformationRules = false;
            // 
            // reportsModule1
            // 
            this.reportsModule1.EnableInplaceReports = true;
            this.reportsModule1.ReportDataType = typeof(DevExpress.Persistent.BaseImpl.ReportData);
            // 
            // pivotChartModuleBase1
            // 
            this.pivotChartModuleBase1.ShowAdditionalNavigation = false;
            // 
            // CashDisciplineWindowsFormsApplication
            // 
            this.ApplicationName = "CashDiscipline";
            this.Connection = this.sqlConnection1;
            this.Modules.Add(this.module1);
            this.Modules.Add(this.module2);
            this.Modules.Add(this.businessClassLibraryCustomizationModule1);
            this.Modules.Add(this.cloneObjectModule1);
            this.Modules.Add(this.viewVariantsModule1);
            this.Modules.Add(this.validationModule1);
            this.Modules.Add(this.pivotGridModule1);
            this.Modules.Add(this.reportsModule1);
            this.Modules.Add(this.pivotGridLayoutModule1);
            this.Modules.Add(this.XafologySystemModule1);
            this.Modules.Add(this.xpoModule1);
            this.Modules.Add(this.layoutModule1);
            this.Modules.Add(this.msoExcelModule1);
            this.Modules.Add(this.importModule1);
            this.Modules.Add(this.module3);
            this.Modules.Add(this.scriptRecorderModuleBase1);
            this.Modules.Add(this.scriptRecorderWindowsFormsModule1);
            this.Modules.Add(this.reportsWindowsFormsModule1);
            this.Modules.Add(this.pivotGridWindowsFormsModule1);
            this.Modules.Add(this.pivotChartModuleBase1);
            this.Modules.Add(this.pivotChartWindowsFormsModule1);
            this.Modules.Add(this.CashDisciplinePivotGridWinModule1);
            this.Modules.Add(this.fileAttachmentsWindowsFormsModule1);
            this.Modules.Add(this.pivotGridLayoutWindowsFormsModule1);
            this.Modules.Add(this.layoutWindowsFormsModule1);
            this.Modules.Add(this.XafologySystemWindowsFormsModule1);
            this.Modules.Add(this.batchDeleteWinModule1);
            this.Modules.Add(this.pasteWinModule1);
            this.Modules.Add(this.module4);
            this.DatabaseVersionMismatch += new System.EventHandler<DevExpress.ExpressApp.DatabaseVersionMismatchEventArgs>(this.CashDisciplineWindowsFormsApplication_DatabaseVersionMismatch);
            this.CustomizeLanguagesList += new System.EventHandler<DevExpress.ExpressApp.CustomizeLanguagesListEventArgs>(this.CashDisciplineWindowsFormsApplication_CustomizeLanguagesList);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.ExpressApp.SystemModule.SystemModule module1;
        private DevExpress.ExpressApp.Win.SystemModule.SystemWindowsFormsModule module2;
        private CashDiscipline.Module.CashDisciplineModule module3;
        private CashDiscipline.Module.Win.CashDisciplineWindowsFormsModule module4;
        private System.Data.SqlClient.SqlConnection sqlConnection1;
        private DevExpress.ExpressApp.Objects.BusinessClassLibraryCustomizationModule businessClassLibraryCustomizationModule1;
        private DevExpress.ExpressApp.CloneObject.CloneObjectModule cloneObjectModule1;
        private DevExpress.ExpressApp.ViewVariantsModule.ViewVariantsModule viewVariantsModule1;
        private DevExpress.ExpressApp.Validation.ValidationModule validationModule1;
        private DevExpress.ExpressApp.PivotGrid.PivotGridModule pivotGridModule1;
        private DevExpress.ExpressApp.Reports.ReportsModule reportsModule1;
        private DevExpress.ExpressApp.ScriptRecorder.ScriptRecorderModuleBase scriptRecorderModuleBase1;
        private DevExpress.ExpressApp.ScriptRecorder.Win.ScriptRecorderWindowsFormsModule scriptRecorderWindowsFormsModule1;
        private DevExpress.ExpressApp.Reports.Win.ReportsWindowsFormsModule reportsWindowsFormsModule1;
        private DevExpress.ExpressApp.PivotGrid.Win.PivotGridWindowsFormsModule pivotGridWindowsFormsModule1;
        private DevExpress.ExpressApp.PivotChart.PivotChartModuleBase pivotChartModuleBase1;
        private DevExpress.ExpressApp.PivotChart.Win.PivotChartWindowsFormsModule pivotChartWindowsFormsModule1;
        private Xafology.ExpressApp.PivotGrid.Win.XafologyPivotGridWinModule CashDisciplinePivotGridWinModule1;
        private Xafology.ExpressApp.SystemModule.XafologySystemModule XafologySystemModule1;
        private DevExpress.ExpressApp.FileAttachments.Win.FileAttachmentsWindowsFormsModule fileAttachmentsWindowsFormsModule1;
        private Xafology.ExpressApp.PivotGridLayout.PivotGridLayoutModule pivotGridLayoutModule1;
        private Xafology.ExpressApp.Xpo.XpoModule xpoModule1;
        private Xafology.ExpressApp.PivotGridLayout.Win.PivotGridLayoutWindowsFormsModule pivotGridLayoutWindowsFormsModule1;
        private Xafology.ExpressApp.Layout.LayoutModule layoutModule1;
        private Xafology.ExpressApp.Layout.Win.LayoutWindowsFormsModule layoutWindowsFormsModule1;
        private Xafology.ExpressApp.Win.SystemModule.XafologySystemWindowsFormsModule XafologySystemWindowsFormsModule1;
        private Xafology.ExpressApp.MsoExcel.MsoExcelModule msoExcelModule1;
        private Xafology.ExpressApp.Xpo.Import.ImportModule importModule1;
        private Xafology.ExpressApp.BatchDelete.Win.BatchDeleteWinModule batchDeleteWinModule1;
        private Xafology.ExpressApp.Paste.Win.PasteWinModule pasteWinModule1;
    }
}
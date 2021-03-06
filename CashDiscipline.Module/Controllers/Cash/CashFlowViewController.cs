﻿using CashDiscipline.Module.BusinessObjects.Cash;
using CashDiscipline.Module.Logic.Cash;
using CashDiscipline.Module.Controllers.Forex;
using CashDiscipline.Module.ParamObjects.Cash;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Xpo;
using System;
using System.Collections.Generic;
using DG2NTT.AnalysisServicesHelpers;
using Xafology.ExpressApp.SystemModule;
using System.Data.SqlClient;

namespace CashDiscipline.Module.Controllers.Cash
{
    public class CashFlowViewController : ViewController
    {

        public const string processCubeCaption = "Process Report";  
        public const string processCubeSshotCaption = "Process Snapshots";
        public const string mapSelectedCaption = "Map Selected";
        public const string fixForecastFormCaption = "Fix Forecast";
        public const string ActionId = "RunCashFlowProgramAction";
        public const string saveForecastCaption = "Save Forecast";

        public SingleChoiceAction CashFlowActions;

        public CashFlowViewController()
        {
            TargetObjectType = typeof(CashFlow);

            CashFlowActions = new SingleChoiceAction(this, ActionId, DevExpress.Persistent.Base.PredefinedCategory.Edit);
            CashFlowActions.Caption = "Actions";
            CashFlowActions.ItemType = SingleChoiceActionItemType.ItemIsOperation;
            CashFlowActions.Execute += runProgramAction_Execute;
            CashFlowActions.ShowItemsOnClick = true;

            var dailyUpdateAction = new ChoiceActionItem();
            dailyUpdateAction.Caption = "Daily Update";
            CashFlowActions.Items.Add(dailyUpdateAction);

            var mapAction = new ChoiceActionItem();
            mapAction.Caption = mapSelectedCaption;
            CashFlowActions.Items.Add(mapAction);

            var fixForecastAction = new ChoiceActionItem();
            fixForecastAction.Caption = "Fix Forecast";
            CashFlowActions.Items.Add(fixForecastAction);

            var reloadForexTradesAction = new ChoiceActionItem();
            reloadForexTradesAction.Caption = "Reload Forex Forecast";
            CashFlowActions.Items.Add(reloadForexTradesAction);

            var saveForecastAction = new ChoiceActionItem();
            saveForecastAction.Caption = saveForecastCaption;
            CashFlowActions.Items.Add(saveForecastAction);

            var processReportAction = new ChoiceActionItem();
            processReportAction.Caption = processCubeCaption;
            CashFlowActions.Items.Add(processReportAction);

        }

        protected override void OnActivated()
        {
            base.OnActivated();
            ObjectSpace.ObjectSaving += ObjectSpace_ObjectSaving;

            

        }

        private void ObjectSpace_ObjectSaving(object sender, ObjectManipulatingEventArgs e)
        {
            var cashFlow = e.Object as CashFlow;
            if (cashFlow != null && !cashFlow.IsDeleted)
            {

            }
        }

        void runProgramAction_Execute(object sender, SingleChoiceActionExecuteEventArgs e)
        {
            var choice = e.SelectedChoiceActionItem;
            var captionPath = choice.GetCaptionPath();

            switch (e.SelectedChoiceActionItem.Caption)
            {
                case "Daily Update":
                    ShowDailyCashUpdateForm(e.ShowViewParameters);
                    break;
                case fixForecastFormCaption:
                    ShowFixForecastForm(e.ShowViewParameters);
                    break;
                case mapSelectedCaption:
                    MapSelected();
                    break;
                case "Reload Forex Forecast":
                    var uploader = new CashDiscipline.Module.Logic.Forex.ForexToCashFlowUploader((XPObjectSpace)ObjectSpace);
                    uploader.Process();
                    ObjectSpace.CommitChanges();
                    break;
                case "Save Forecast":
                    SaveForecast();
                    break;
                case "Reindex":
                    Reindex();
                    break;
                default:
                    if (e.SelectedChoiceActionItem.Caption == processCubeCaption)
                    {
                        ProcessCube(e.SelectedChoiceActionItem.Caption);
                    }
                    break;
            }
        }

        public void Reindex()
        {
            var objSpace = (XPObjectSpace)ObjectSpace;
            objSpace.Session.PurgeDeletedObjects();


        }

        public bool ProcessCube(string caption)
        {
            var tabular = new CashFlowTabular();

            if (ObjectSpace.IsModified)
            {
                new GenericMessageBox(
                    "Please save your changes before processing the Cash Report", "Cash Report Processing Cancelled",
                    (app, svp) => { return; });
                return false;
            }
            switch (caption)
            {
                case processCubeCaption:
                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    tabular.ProcessAll();
                    sw.Stop();
                    new GenericMessageBox(
                         string.Format(
                            "ACTION COMPLETED : Process Cash Report - All : Elapsed = {0} seconds",
                            Math.Round(sw.Elapsed.TotalSeconds,2)),
                         "ACTION COMPLETED");
                    break;
                case processCubeSshotCaption:
                    tabular.ProcessSshot();
                    new GenericMessageBox(
                        "ACTION COMPLETED : Process Cash Report - Snapshots",
                        "ACTION COMPLETED");
                    break;
            }
            return true;
        }

        private void ShowFixForecastForm(ShowViewParameters svp)
        {

            var os = Application.CreateObjectSpace();
            var paramObj = CashFlowFixParam.GetInstance(os);
            var detailView = Application.CreateDetailView(os, paramObj);
            svp.CreatedView = detailView;
        }

        private void ShowDailyCashUpdateForm(ShowViewParameters svp)
        {
            var os = Application.CreateObjectSpace();
            var paramObj = DailyCashUpdateParam.GetInstance(os);
            var detailView = Application.CreateDetailView(os, paramObj);
            svp.TargetWindow = TargetWindow.NewModalWindow;
            svp.CreatedView = detailView;
        }

        private void SaveForecast()
        {
            var objSpace = (XPObjectSpace)Application.CreateObjectSpace();
            var logic = new SaveForecastSnapshot(objSpace);
            logic.Process();
            new GenericMessageBox(
                 "ACTION COMPLETED : Save Forecast",
                 "ACTION COMPLETED");
        }

        private void MapSelected()
        {
            var mapper = new CashFlowFixMapper((XPObjectSpace)ObjectSpace);
            var cashFlows = View.SelectedObjects;
            mapper.Process(cashFlows);

            new Xafology.ExpressApp.SystemModule.GenericMessageBox(
            "ACTION COMPLETED : Map Selected",
            "ACTION COMPLETED");
        }
    }
}

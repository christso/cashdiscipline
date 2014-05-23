﻿using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CTMS.Module.ParamObjects.Import;
using System.Diagnostics;
using CTMS.Module.BusinessObjects.Cash;
using System.IO;
using LumenWorks.Framework.IO.Csv;
using DevExpress.Xpo;
using DevExpress.ExpressApp.Xpo;
using D2NXAF.Utilities;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Actions;
using OfficeOpenXml;
using CTMS.Module.HelperClasses;
using CTMS.Module.BusinessObjects;

namespace CTMS.Module.Controllers
{
    public class ImportDelimitedFileDataViewController : ViewControllerEx
    {
        public ImportDelimitedFileDataViewController()
        {
            TargetObjectType = typeof(ImportDelimitedFileDataParam);
            TargetViewType = ViewType.DetailView;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            ((DetailView)View).ViewEditMode = ViewEditMode.Edit;
            var dc = Frame.GetController<DialogController>();
            if (dc != null)
            {
                dc.AcceptAction.Execute += AcceptAction_Execute;
                dc.CancelAction.Execute += CancelAction_Execute;
                dc.CanCloseWindow = false;
            }
        }

        protected void CancelAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            View.Close();
        }

        private string QuoteJoinString(string separator, string quote, IEnumerable<string> values)
        {
            string result = "";
            foreach (string value in values)
            {
                if (result != "")
                    result += separator;
                result += " " + quote + value + quote;
            }
            return result;
        }

        private void FastCsvImport()
        {
            var paramObj = (ImportDelimitedFileDataParam)View.CurrentObject;
            if (paramObj.File.Content == null)
                throw new UserFriendlyException("No file was selected to upload.");
            var byteArray = paramObj.File.Content;
            var stream = new MemoryStream(byteArray);
            bool hasHeaders = true;
            var objSpace = (XPObjectSpace)Application.CreateObjectSpace();
            var objTypeInfo = paramObj.ObjectTypeInfo;

            Action job = new Action(() =>
            {
                var engine = new CTMS.Module.HelperClasses.Xpo.CsvXpoEngine(Application);
                bool userTriggersEnabled = AppSettings.UserTriggersEnabled;
                try
                {
                    AppSettings.UserTriggersEnabled = false;
                    engine.CancellationTokenSource = this.CancellationTokenSource;
                    engine.Options.CreateMembers = paramObj.CreateMembers;
                    engine.Options.CacheObjects = paramObj.CacheLookupObjects;
                    if (paramObj.ImportActionType == ImportActionType.Insert)
                        engine.ImportCsvFileInserts(stream, objSpace, objTypeInfo, hasHeaders, paramObj.FieldMaps);
                    else if (paramObj.ImportActionType == ImportActionType.Update)
                        engine.ImportCsvFileUpdates(stream, objSpace, objTypeInfo, hasHeaders, paramObj.FieldMaps);

                    if (engine.XpObjectsNotFound.Count != 0)
                    {
                        string messageFormat;
                        if (paramObj.CreateMembers)
                            messageFormat = "Members created: {0} = {1}";
                        else
                            messageFormat = "Members not found: {0} = {1}";
                        foreach (var pair in engine.XpObjectsNotFound)
                        {
                            WriteLogLine(string.Format(messageFormat, pair.Key, QuoteJoinString(",", "'", pair.Value)), false);
                            WriteLogLine("", false);
                        }
                    }
                }
                catch (D2NXAF.Utilities.Data.ConvertException)
                {
                    CustomRequestExitStatus = RequestStatus.Error;

                    if (engine.ErrorInfo != null)
                    {
                        WriteLogLine(string.Format("Line: {0}. Field: {1}. Error converting '{2}' to type: '{3}'. {4}",
                            engine.ErrorInfo.LineNumber,
                            engine.ErrorInfo.ColumnName,
                            engine.ErrorInfo.OrigValue,
                            engine.ErrorInfo.ColumnType,
                            engine.ErrorInfo.ExceptionInfo.Message), false);
                        WriteLogLine("", false);
                    }
                }
                catch (InvalidDataException)
                {
                    if (engine.ErrorInfo == null) throw;
                    CustomRequestExitStatus = RequestStatus.Error;
                    WriteLogLine(string.Format("Line: {0}. {2}",
                        engine.ErrorInfo.ExceptionInfo.Message), false);
                    WriteLogLine("", false);
                }
                finally
                {
                    AppSettings.UserTriggersEnabled = userTriggersEnabled;
                }
            });
            SubmitRequest("Import CSV File", job);
        }

        protected void AcceptAction_Execute(object sender, DevExpress.ExpressApp.Actions.SimpleActionExecuteEventArgs e)
        {
            var paramObj = (ImportDelimitedFileDataParam)View.CurrentObject;
            switch (paramObj.ImportLibrary)
            {
                case ImportLibrary.FastCsvReader:
                    FastCsvImport();
                    break;
            }
        }
    }
}

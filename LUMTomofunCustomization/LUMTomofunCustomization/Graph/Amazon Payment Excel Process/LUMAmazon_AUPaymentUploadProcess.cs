﻿using LumTomofunCustomization.LUMLibrary;
using LUMTomofunCustomization.DAC;
using PX.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LumTomofunCustomization.Graph
{
    public class LUMAmazon_AUPaymentUploadProcess : PXGraph<LUMAmazon_AUPaymentUploadProcess>, PXImportAttribute.IPXPrepareItems
    {
        public PXSave<LUMAmazonAUPaymentReport> Save;
        public PXCancel<LUMAmazonAUPaymentReport> Cancel;

        public PXFilter<AmazonPaymentUploadFileter> Filter;
        [PXImport(typeof(LUMAmazonAUPaymentReport))]
        public PXProcessing<LUMAmazonAUPaymentReport> PaymentTransactions;

        private static Object thisLock = new Object();

        protected virtual IEnumerable filter()
        {
            AmazonPaymentUploadFileter filter = Filter.Current;
            int startRow = 0;
            int totalRows = 0;
            if (filter != null)
            {
                filter.ApiTotal = 0; //reset total to zero 
                var cmd = PaymentTransactions.View.BqlSelect.AggregateNew<Aggregate<Sum<LUMAmazonAUPaymentReport.api_total>>>();// add aggregation to the existing BQL
                foreach (LUMAmazonAUPaymentReport row in
                        new PXView(this, false, cmd).Select(null, //pass filter context to the grid view delegate
                        null,
                        null,
                        null,
                        null,
                        PaymentTransactions.View.GetExternalFilters(), //Get grid user filters
                        ref startRow,
                        0, //get all records without regard to paging
                        ref totalRows))
                {
                    filter.ApiTotal += row.Api_total ?? 0;
                }
            }
            yield return filter;
        }

        public LUMAmazon_AUPaymentUploadProcess()
        {
            this.PaymentTransactions.Cache.AllowInsert = this.PaymentTransactions.Cache.AllowUpdate = this.PaymentTransactions.Cache.AllowDelete = true;
            #region Set Field Enable
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.reportDateTime>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.settlementid>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.reportType>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.orderID>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.sku>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.description>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.productSales>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.productSalesTax>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.shippingCredits>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.giftWrapCredits>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.promotionalRebates>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.marketplaceWithheldTax>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.sellingFees>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.fbafees>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.otherTransactionFee>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.otherFee>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonAUPaymentReport.total>(PaymentTransactions.Cache, null, true);
            #endregion
            this.PaymentTransactions.SetProcessDelegate(delegate (List<LUMAmazonAUPaymentReport> list)
            {
                GoProcessing(list);
            });
            this.PaymentTransactions.ParallelProcessingOptions = (options) =>
            {
                options.BatchSize = 100;
                options.IsEnabled = true;
            };
        }

        #region Action
        public PXAction<LUMAmazonAUPaymentReport> deleteRecord;
        [PXButton]
        [PXUIField(DisplayName = "Delete All Payment", MapEnableRights = PXCacheRights.Delete, MapViewRights = PXCacheRights.Delete)]
        public virtual IEnumerable DeleteRecord(PXAdapter adapter)
        {
            WebDialogResult result = this.PaymentTransactions.Ask(ActionsMessages.Warning, PXMessages.LocalizeFormatNoPrefix("Do you want to delete data?"),
                MessageButtons.OKCancel, MessageIcon.Warning, true);
            if (result != WebDialogResult.OK)
                return adapter.Get();

            PXDatabase.Delete<LUMAmazonAUPaymentReport>();
            this.PaymentTransactions.Cache.Clear();
            return adapter.Get();
        }

        #endregion

        #region Event

        public virtual void _(Events.RowInserted<LUMAmazonAUPaymentReport> e)
        {
            var row = e.Row as LUMAmazonAUPaymentReport;
            #region API Field Binding
            row.API_Marketplace = "AU";
            var CultureName = "en-AU";
            row.Api_date_1 = AmazonPublicFunction.DatetimeParseWithCulture(CultureName, row.ReportDateTime);
            row.Api_date = AmazonPublicFunction.DatetimeParseWithCulture(CultureName, row.ReportDateTime);
            row.Api_settlementid = row?.Settlementid;
            row.Api_trantype = AmazonPublicFunction.AmazonOrderTypeTreanslate(row.API_Marketplace, row?.ReportType);
            row.Api_orderid = row?.OrderID;
            row.Api_sku = row?.Sku;
            row.Api_description = row?.Description;
            row.Api_productsales = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.ProductSales) ? "0" : row.ProductSales);
            row.Api_producttax = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.ProductSalesTax) ? "0" : row?.ProductSalesTax);
            row.Api_shipping = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.ShippingCredits) ? "0" : row?.ShippingCredits);
            row.Api_shippingtax = 0;
            row.Api_giftwrap = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.GiftWrapCredits) ? "0" : row?.GiftWrapCredits);
            row.Api_giftwraptax = 0;
            row.Api_regulatoryfee = 0;
            row.Api_taxonregulatoryfee = 0;
            row.Api_promotion = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.PromotionalRebates) ? "0" : row?.PromotionalRebates);
            row.Api_promotiontax = 0;
            row.Api_whtax = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.MarketplaceWithheldTax) ? "0" : row?.MarketplaceWithheldTax);
            row.Api_sellingfee = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.SellingFees) ? "0" : row?.SellingFees);
            row.Api_fbafee = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.Fbafees) ? "0" : row?.Fbafees);
            row.Api_othertranfee = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.OtherTransactionFee) ? "0" : row?.OtherTransactionFee);
            row.Api_otherfee = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.OtherFee) ? "0" : row?.OtherFee);
            row.Api_total = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.Total) ? "0" : row?.Total);
            row.Api_cod = 0;
            row.Api_codfee = 0;
            row.Api_coditemcharge = 0;
            row.Api_points = 0;
            #endregion
        }

        #endregion

        #region Method
        public static void GoProcessing(List<LUMAmazonAUPaymentReport> list)
        {
            var baseGraph = CreateInstance<LUMAmazon_AUPaymentUploadProcess>();
            baseGraph.CreatePaymentByOrder(baseGraph, list);
        }

        /// <summary> 執行 Process Amazon payment </summary>
        public virtual void CreatePaymentByOrder(LUMAmazon_AUPaymentUploadProcess baseGraph, List<LUMAmazonAUPaymentReport> selectedList)
        {
            foreach (var selectedItem in selectedList)
            {
                // Initial variable
                string errorMessge = string.Empty;
                // Setting Process Current item
                PXProcessing.SetCurrentItem(selectedItem);
                try
                {
                    // Skip 
                    if (selectedItem.IsProcessed ?? false)
                        continue;
                    using (PXTransactionScope sc = new PXTransactionScope())
                    {
                        AmazonToAcumaticaCore<LUMAmazonAUPaymentReport>.CreatePayment(selectedItem, selectedItem.API_Marketplace, baseGraph);
                        sc.Complete();
                    }
                }
                catch (PXOuterException outerException)
                {
                    for (int i = 0; i < outerException.InnerFields.Length; i++)
                        errorMessge += $"{outerException.InnerFields[i]} - {outerException.InnerMessages[i]} \r\n";
                }
                catch (Exception ex)
                {
                    errorMessge = ex.Message;
                }
                finally
                {
                    // Setting record information
                    selectedItem.ErrorMessage = errorMessge.Length > 2048 ? errorMessge.Substring(0, 2048) : errorMessge; ;
                    selectedItem.IsProcessed = string.IsNullOrEmpty(errorMessge);
                    baseGraph.PaymentTransactions.Update(selectedItem);
                    // Setting Process information
                    if (!string.IsNullOrEmpty(errorMessge))
                        PXProcessing.SetError(errorMessge);
                    else
                        PXProcessing.SetProcessed();
                }
            }
            lock (thisLock)
                baseGraph.Actions.PressSave();
        }

        #endregion

        public bool PrepareImportRow(string viewName, IDictionary keys, IDictionary values)
            => true;

        public void PrepareItems(string viewName, IEnumerable items) { }

        public bool RowImported(string viewName, object row, object oldRow)
            => true;

        public bool RowImporting(string viewName, object row)
            => true;
    }
}

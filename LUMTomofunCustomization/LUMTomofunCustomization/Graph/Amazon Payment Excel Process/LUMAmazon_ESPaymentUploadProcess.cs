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
    public class LUMAmazon_ESPaymentUploadProcess : PXGraph<LUMAmazon_ESPaymentUploadProcess>, PXImportAttribute.IPXPrepareItems
    {
        public PXSave<LUMAmazonESPaymentReport> Save;
        public PXCancel<LUMAmazonESPaymentReport> Cancel;

        public PXFilter<AmazonPaymentUploadFileter> Filter;
        [PXImport(typeof(LUMAmazonESPaymentReport))]
        public PXProcessing<LUMAmazonESPaymentReport> PaymentTransactions;

        private static Object thisLock = new Object();

        protected virtual IEnumerable filter()
        {
            AmazonPaymentUploadFileter filter = Filter.Current;
            int startRow = 0;
            int totalRows = 0;
            if (filter != null)
            {
                filter.ApiTotal = 0; //reset total to zero 
                var cmd = PaymentTransactions.View.BqlSelect.AggregateNew<Aggregate<Sum<LUMAmazonESPaymentReport.api_total>>>();// add aggregation to the existing BQL
                foreach (LUMAmazonESPaymentReport row in
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

        public LUMAmazon_ESPaymentUploadProcess()
        {
            this.PaymentTransactions.Cache.AllowInsert = this.PaymentTransactions.Cache.AllowUpdate = this.PaymentTransactions.Cache.AllowDelete = true;
            #region Set Field Enable
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.reportDateTime>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.settlementid>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.reportType>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.orderID>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.sku>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.description>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.productSales>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.productSalesTax>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.shippingCredits>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.shippingCreditsTax>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.giftWrapCredits>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.giftWrapCreditsTax>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.promotionalRebates>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.promotionalRebatesTax>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.marketplaceWithheldTax>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.sellingFees>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.fbafees>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.otherTransactionFee>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.otherFee>(PaymentTransactions.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMAmazonESPaymentReport.total>(PaymentTransactions.Cache, null, true);
            #endregion
            this.PaymentTransactions.SetProcessDelegate(delegate (List<LUMAmazonESPaymentReport> list)
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
        public PXAction<LUMAmazonESPaymentReport> deleteRecord;
        [PXButton]
        [PXUIField(DisplayName = "Delete All Payment", MapEnableRights = PXCacheRights.Delete, MapViewRights = PXCacheRights.Delete)]
        public virtual IEnumerable DeleteRecord(PXAdapter adapter)
        {
            WebDialogResult result = this.PaymentTransactions.Ask(ActionsMessages.Warning, PXMessages.LocalizeFormatNoPrefix("Do you want to delete data?"),
                MessageButtons.OKCancel, MessageIcon.Warning, true);
            if (result != WebDialogResult.OK)
                return adapter.Get();

            PXDatabase.Delete<LUMAmazonESPaymentReport>();
            this.PaymentTransactions.Cache.Clear();
            return adapter.Get();
        }

        #endregion

        #region Event

        public virtual void _(Events.RowInserted<LUMAmazonESPaymentReport> e)
        {
            var row = e.Row as LUMAmazonESPaymentReport;
            #region API Field Binding
            row.API_Marketplace = "ES";
            var CultureName = "es-ES";
            var marketplacePreference = AmazonPublicFunction.GetMarketplacePreference(row.API_Marketplace);
            row.Api_date_1 = AmazonPublicFunction.DatetimeParseWithCulture(CultureName, row.ReportDateTime);
            row.Api_date = AmazonPublicFunction.DatetimeParseWithCulture(CultureName, row.ReportDateTime)?.AddHours(marketplacePreference?.TimeZone ?? 0);
            row.Api_settlementid = row?.Settlementid;
            row.Api_trantype = AmazonPublicFunction.AmazonOrderTypeTreanslate(row.API_Marketplace, row?.ReportType);
            row.Api_orderid = row?.OrderID;
            row.Api_sku = row?.Sku;
            row.Api_description = row?.Description;
            row.Api_productsales = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.ProductSales) ? "0" : row.ProductSales);
            row.Api_producttax = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.ProductSalesTax) ? "0" : row?.ProductSalesTax);
            row.Api_shipping = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.ShippingCredits) ? "0" : row?.ShippingCredits);
            row.Api_shippingtax = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.ShippingCreditsTax) ? "0" : row?.ShippingCreditsTax);
            row.Api_giftwrap = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.GiftWrapCredits) ? "0" : row?.GiftWrapCredits);
            row.Api_giftwraptax = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.GiftWrapCreditsTax) ? "0" : row?.GiftWrapCreditsTax);
            row.Api_regulatoryfee = 0;
            row.Api_taxonregulatoryfee = 0;
            row.Api_promotion = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.PromotionalRebates) ? "0" : row?.PromotionalRebates);
            row.Api_promotiontax = AmazonPublicFunction.CurrencyConvertWithCulture(CultureName, string.IsNullOrEmpty(row?.PromotionalRebatesTax) ? "0" : row?.PromotionalRebatesTax);
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
        public static void GoProcessing(List<LUMAmazonESPaymentReport> list)
        {
            var baseGraph = CreateInstance<LUMAmazon_ESPaymentUploadProcess>();
            baseGraph.CreatePaymentByOrder(baseGraph, list);
        }

        /// <summary> 執行 Process Amazon payment </summary>
        public virtual void CreatePaymentByOrder(LUMAmazon_ESPaymentUploadProcess baseGraph, List<LUMAmazonESPaymentReport> selectedList)
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
                        AmazonToAcumaticaCore<LUMAmazonESPaymentReport>.CreatePayment(selectedItem, selectedItem.API_Marketplace, baseGraph);
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

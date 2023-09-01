﻿using LUMTomofunCustomization.DAC;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Objects.SO;
using PX.Data.BQL;
using LumTomofunCustomization.LUMLibrary;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.Objects.IN;
using PX.Objects.SO.GraphExtensions.SOOrderEntryExt;
using PX.Objects.SO.GraphExtensions.ARPaymentEntryExt;

namespace LumTomofunCustomization.Graph
{
    public class LUMShopifySettlementTransactionProcess : PXGraph<LUMShopifySettlementTransactionProcess>, PXImportAttribute.IPXPrepareItems
    {
        public PXSave<LUMShopifySettlementTransData> Save;
        public PXCancel<LUMShopifySettlementTransData> Cancel;

        [PXImport(typeof(LUMShopifySettlementTransData))]
        public PXProcessing<LUMShopifySettlementTransData> SettlementTransaction;

        public LUMShopifySettlementTransactionProcess()
        {
            this.SettlementTransaction.Cache.AllowInsert = this.SettlementTransaction.Cache.AllowUpdate = this.SettlementTransaction.Cache.AllowDelete = true;
            #region Set Field Enable
            PXUIFieldAttribute.SetEnabled<LUMShopifySettlementTransData.transSequenceNumber>(SettlementTransaction.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMShopifySettlementTransData.marketplace>(SettlementTransaction.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMShopifySettlementTransData.transactionDate>(SettlementTransaction.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMShopifySettlementTransData.transactionType>(SettlementTransaction.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMShopifySettlementTransData.orderID>(SettlementTransaction.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMShopifySettlementTransData.payoutDate>(SettlementTransaction.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMShopifySettlementTransData.amount>(SettlementTransaction.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMShopifySettlementTransData.fee>(SettlementTransaction.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMShopifySettlementTransData.net>(SettlementTransaction.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMShopifySettlementTransData.checkout>(SettlementTransaction.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMShopifySettlementTransData.paymentMethodName>(SettlementTransaction.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMShopifySettlementTransData.currency>(SettlementTransaction.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMShopifySettlementTransData.presentmentAmount>(SettlementTransaction.Cache, null, true);
            PXUIFieldAttribute.SetEnabled<LUMShopifySettlementTransData.presentmentCurrency>(SettlementTransaction.Cache, null, true);
            #endregion
            this.SettlementTransaction.SetProcessDelegate(delegate (List<LUMShopifySettlementTransData> list)
            {
                GoProcessing(list);
            });
        }

        #region Action
        public PXAction<LUMShopifySettlementTransData> deleteRecord;
        [PXButton]
        [PXUIField(DisplayName = "Delete All Payment", MapEnableRights = PXCacheRights.Delete, MapViewRights = PXCacheRights.Delete)]
        public virtual IEnumerable DeleteRecord(PXAdapter adapter)
        {
            WebDialogResult result = this.SettlementTransaction.Ask(ActionsMessages.Warning, PXMessages.LocalizeFormatNoPrefix("Do you want to delete data?"),
                MessageButtons.OKCancel, MessageIcon.Warning, true);
            if (result != WebDialogResult.OK)
                return adapter.Get();

            PXDatabase.Delete<LUMShopifySettlementTransData>();
            this.SettlementTransaction.Cache.Clear();
            return adapter.Get();
        }

        #endregion

        #region Method

        public static void GoProcessing(List<LUMShopifySettlementTransData> list)
        {
            var baseGraph = CreateInstance<LUMShopifySettlementTransactionProcess>();
            baseGraph.CreateShopifyPayment(list, baseGraph);
        }

        /// <summary> Create Shopify Payment </summary>
        public virtual void CreateShopifyPayment(List<LUMShopifySettlementTransData> list, LUMShopifySettlementTransactionProcess baseGraph)
        {
            foreach (var row in list)
            {
                // clean error message
                row.Marketplace = row.OrderID.StartsWith("#") ? row.OrderID?.Substring(1, 2) : row.OrderID?.Substring(0, 2);
                if (string.IsNullOrEmpty(row.Marketplace))
                    throw new Exception("Marketplace can not be empty!");
                row.ErrorMessage = string.Empty;
                PXProcessing.SetCurrentItem(row);
                // Marketplace tax calculation
                var isTaxCalculate = ShopifyPublicFunction.GetMarketplaceTaxCalculation(row.Marketplace);
                try
                {
                    using (PXTransactionScope sc = new PXTransactionScope())
                    {
                        // SOLine SalesAccount
                        int? newSalesAcctID = null;
                        // SOLine SalesSubAccount
                        int? newSalesSubAcctID = null;
                        // 以建立的Shopify Sales Order
                        var oldShopifySOOrder = SelectFrom<SOOrder>
                         .Where<SOOrder.orderType.IsEqual<P.AsString>
                           .And<SOOrder.customerOrderNbr.IsEqual<P.AsString>>>
                         .View.SelectSingleBound(baseGraph, null, "SP", row.Checkout?.Substring(1)).TopFirst;
                        // Shopify Cash account
                        var spCashAccount = SelectFrom<CashAccount>
                                            .Where<CashAccount.cashAccountCD.IsEqual<P.AsString>>
                                            .View.SelectSingleBound(baseGraph, null, $"{row.Marketplace}SPFSPF").TopFirst;
                        switch (row.TransactionType.ToUpper())
                        {
                            case "CHARGE":
                                #region TransactionType: CHARGE
                                if (oldShopifySOOrder == null)
                                    throw new PXException("Cannot find Sales Order");
                                if (spCashAccount == null)
                                    throw new PXException($"Can not find Cash Account ({row.Currency}SPFSPF)");
                                var arGraph = PXGraph.CreateInstance<ARPaymentEntry>();

                                #region Header(Document)
                                var arDoc = arGraph.Document.Cache.CreateInstance() as ARPayment;
                                arDoc.DocType = oldShopifySOOrder == null ? "PMT" :
                                                oldShopifySOOrder.Status == "N" ? "PPM" : "PMT";
                                arDoc.AdjDate = row.TransactionDate;
                                arDoc.ExtRefNbr = row.Checkout;
                                arDoc.CustomerID = ShopifyPublicFunction.GetMarketplaceCustomer(row.Marketplace);
                                arDoc.CashAccountID = spCashAccount.CashAccountID;
                                arDoc.DocDesc = $"Shopify Payment Gateway {row.Checkout}";
                                arDoc.DepositAfter = row.PayoutDate;
                                #region User-Defiend
                                // UserDefined - ECNETPAY
                                arGraph.Document.Cache.SetValueExt(arDoc, PX.Objects.CS.Messages.Attribute + "ECNETPAY", row.Net);
                                #endregion

                                arGraph.Document.Insert(arDoc);
                                #endregion

                                #region Document to Apply / Sales order
                                if (arDoc.DocType == "PMT")
                                {
                                    #region Adjustments
                                    var adjTrans = arGraph.Adjustments.Cache.CreateInstance() as ARAdjust;
                                    adjTrans.AdjdDocType = "INV";
                                    adjTrans.AdjdRefNbr = SelectFrom<ARInvoice>
                                                          .InnerJoin<ARTran>.On<ARInvoice.docType.IsEqual<ARTran.tranType>
                                                                .And<ARInvoice.refNbr.IsEqual<ARTran.refNbr>>>
                                                          .InnerJoin<SOOrder>.On<ARTran.sOOrderNbr.IsEqual<SOOrder.orderNbr>>
                                                          .Where<SOOrder.orderNbr.IsEqual<P.AsString>
                                                            .And<SOOrder.orderType.IsEqual<P.AsString>>>
                                                          .View.SelectSingleBound(baseGraph, null, oldShopifySOOrder.OrderNbr, oldShopifySOOrder.OrderType).TopFirst?.RefNbr;
                                    arGraph.Adjustments.Insert(adjTrans);
                                    #endregion
                                }
                                else
                                {
                                    #region SOAdjust
                                    var adjSOTrans = arGraph.GetExtension<OrdersToApplyTab>().SOAdjustments.Cache.CreateInstance() as SOAdjust;
                                    adjSOTrans.AdjdOrderType = oldShopifySOOrder.OrderType;
                                    adjSOTrans.AdjdOrderNbr = oldShopifySOOrder.OrderNbr;
                                    arGraph.GetExtension<OrdersToApplyTab>().SOAdjustments.Insert(adjSOTrans);
                                    #endregion
                                }
                                #endregion

                                #region CHARGS
                                var chargeTrans = arGraph.PaymentCharges.Cache.CreateInstance() as ARPaymentChargeTran;
                                chargeTrans.EntryTypeID = "COMMISSION";
                                chargeTrans.CuryTranAmt = row.Fee ?? 0;
                                arGraph.PaymentCharges.Insert(chargeTrans);
                                #endregion

                                // set payment amount to apply amount
                                if (arDoc.DocType == "PMT")
                                    arGraph.Document.SetValueExt<ARPayment.curyOrigDocAmt>(arGraph.Document.Current, arGraph.Document.Current.CuryApplAmt);
                                else if (arDoc.DocType == "PPM")
                                    arGraph.Document.SetValueExt<ARPayment.curyOrigDocAmt>(arGraph.Document.Current, arGraph.Document.Current.CurySOApplAmt);
                                // Save Payment
                                arGraph.Actions.PressSave();
                                // Remove from hold
                                arGraph.releaseFromHold.Press();
                                // Release
                                arGraph.release.Press();
                                #endregion
                                break;
                            case "REFUND":
                            case "CHARGEBACK":
                                #region TransactionType: REFUND/CHARGEBACK
                                if (oldShopifySOOrder == null && row.TransactionType.ToUpper() == "REFUND")
                                    throw new PXException($"Can not find SalesOrder");
                                var soGraph = PXGraph.CreateInstance<SOOrderEntry>();

                                #region Header
                                var soDoc = soGraph.Document.Cache.CreateInstance() as SOOrder;
                                soDoc.OrderType = "CM";
                                soDoc.CustomerOrderNbr = row?.Checkout + "_" + ShopifyPublicFunction.GetUNIXTimestamp(row?.TransactionDate);
                                soDoc.OrderDate = row.TransactionDate;
                                soDoc.RequestDate = row.TransactionDate;
                                soDoc.CustomerID = ShopifyPublicFunction.GetMarketplaceCustomer(row.Marketplace);
                                soDoc.OrderDesc = $"Shopify Payment Gateway {row.TransactionType} {row.Checkout}";
                                #endregion

                                #region User-Defined
                                // UserDefined - ORDERTYPE
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERTYPE", $"Shopify Refund");
                                // UserDefined - MKTPLACE
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "MKTPLACE", row.Marketplace);
                                // UserDefined - ORDERAMT
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERAMT", Math.Abs(row.Net ?? 0));
                                // UserDefined - ORDTAAMT
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDTAXAMT", 0);
                                // UserDefined - TAXCOLLECT
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "TAXCOLLECT", 0);
                                #endregion

                                // Insert SOOrder
                                soGraph.Document.Insert(soDoc);

                                #region Set Currency
                                CurrencyInfo info = CurrencyInfoAttribute.SetDefaults<SOOrder.curyInfoID>(soGraph.Document.Cache, soGraph.Document.Current);
                                if (info != null)
                                    soGraph.Document.Cache.SetValueExt<SOOrder.curyID>(soGraph.Document.Current, info.CuryID);
                                #endregion

                                #region Address
                                var soGraph_SP = PXGraph.CreateInstance<SOOrderEntry>();
                                soGraph_SP.Document.Current = oldShopifySOOrder;
                                if (soGraph_SP.Document.Current != null)
                                {
                                    // Setting Shipping_Address
                                    var soAddress = soGraph.Shipping_Address.Current;
                                    soGraph_SP.Shipping_Address.Current = soGraph_SP.Shipping_Address.Select();
                                    soAddress.OverrideAddress = true;
                                    soAddress.PostalCode = soGraph_SP.Shipping_Address.Current?.PostalCode;
                                    soAddress.CountryID = soGraph_SP.Shipping_Address.Current?.CountryID;
                                    soAddress.State = soGraph_SP.Shipping_Address.Current?.State;
                                    soAddress.City = soGraph_SP.Shipping_Address.Current?.City;
                                    soAddress.RevisionID = 1;
                                    // Setting Shipping_Contact
                                    var soContact = soGraph.Shipping_Contact.Current;
                                    soGraph_SP.Shipping_Contact.Current = soGraph_SP.Shipping_Contact.Select();
                                    soContact.OverrideContact = true;
                                    soContact.Email = soGraph_SP.Shipping_Contact.Current?.Email;
                                    soContact.RevisionID = 1;
                                }
                                #endregion

                                #region SOLine
                                var ECWHTAXAmount = (decimal?)0;
                                // SOLine-Tax
                                if (row.TransactionType.ToUpper() == "REFUND")
                                {
                                    ECWHTAXAmount = row.Amount * -1 == oldShopifySOOrder?.CuryOrderTotal ? oldShopifySOOrder?.CuryTaxTotal : oldShopifySOOrder?.CuryTaxTotal / oldShopifySOOrder?.CuryOrderTotal * row.Amount * -1;
                                    var itemCD = $"EC-WHTAX-{row.Marketplace}";
                                    var ecSOTran = soGraph.Transactions.Cache.CreateInstance() as SOLine;
                                    ecSOTran.InventoryID = ShopifyPublicFunction.GetInvetoryitemID(soGraph, itemCD);
                                    ecSOTran.OrderQty = 1;
                                    ecSOTran.CuryUnitPrice = ECWHTAXAmount;
                                    newSalesAcctID = ShopifyPublicFunction.GetSalesAcctID(soGraph, itemCD, ecSOTran.InventoryID, oldShopifySOOrder, soDoc.CustomerID);
                                    newSalesSubAcctID = ShopifyPublicFunction.GetSalesSubAcctID(soGraph, itemCD, ecSOTran.InventoryID, oldShopifySOOrder, soDoc.CustomerID);
                                    if (newSalesAcctID.HasValue)
                                        ecSOTran.SalesAcctID = newSalesAcctID;
                                    if (newSalesSubAcctID.HasValue)
                                        ecSOTran.SalesSubID = newSalesSubAcctID;
                                    soGraph.Transactions.Insert(ecSOTran);
                                }
                                // Amount
                                var soTrans = soGraph.Transactions.Cache.CreateInstance() as SOLine;
                                var refundItemCD = row.TransactionType.ToUpper() == "REFUND" && row.Marketplace.ToUpper() == "TW" ? $"Refund-{row.Marketplace}" : row.TransactionType;
                                soTrans.InventoryID = ShopifyPublicFunction.GetInvetoryitemID(soGraph, refundItemCD);
                                soTrans.OrderQty = 1;
                                soTrans.CuryUnitPrice = row.Amount * -1 - ECWHTAXAmount;
                                newSalesAcctID = ShopifyPublicFunction.GetSalesAcctID(soGraph, refundItemCD, soTrans.InventoryID, oldShopifySOOrder, soDoc.CustomerID);
                                newSalesSubAcctID = ShopifyPublicFunction.GetSalesSubAcctID(soGraph, refundItemCD, soTrans.InventoryID, oldShopifySOOrder, soDoc.CustomerID);
                                if (newSalesAcctID.HasValue)
                                    soTrans.SalesAcctID = newSalesAcctID;
                                if (newSalesSubAcctID.HasValue)
                                    soTrans.SalesSubID = newSalesSubAcctID;
                                // If Inventory ID != ‘Refund’ 
                                if (row.TransactionType?.ToUpper() != "REFUND")
                                    soTrans.TaxCategoryID = "NONTAXABLE";
                                soGraph.Transactions.Insert(soTrans);

                                // Fee
                                if ((row?.Fee ?? 0) != 0)
                                {
                                    soTrans = soGraph.Transactions.Cache.CreateInstance() as SOLine;
                                    soTrans.InventoryID = ShopifyPublicFunction.GetInvetoryitemID(soGraph, "EC-COMMISSION");
                                    soTrans.OrderQty = 1;
                                    soTrans.CuryUnitPrice = row.Fee * 1;
                                    newSalesAcctID = ShopifyPublicFunction.GetSalesAcctID(soGraph, "EC-COMMISSION", soTrans.InventoryID, oldShopifySOOrder, soDoc.CustomerID);
                                    newSalesSubAcctID = ShopifyPublicFunction.GetSalesSubAcctID(soGraph, "EC-COMMISSION", soTrans.InventoryID, oldShopifySOOrder, soDoc.CustomerID);
                                    if (newSalesAcctID.HasValue)
                                        soTrans.SalesAcctID = newSalesAcctID;
                                    if (newSalesSubAcctID.HasValue)
                                        soTrans.SalesSubID = newSalesSubAcctID;
                                    // If Inventory ID != ‘Refund’ 
                                    soTrans.TaxCategoryID = "NONTAXABLE";
                                    soGraph.Transactions.Insert(soTrans);
                                }
                                #endregion

                                #region Update Tax
                                if (isTaxCalculate)
                                    soGraph.Document.Cache.SetValue<SOOrder.disableAutomaticTaxCalculation>(soGraph.Document.Current, false);
                                soGraph.Document.UpdateCurrent();

                                // Setting SO Tax
                                soGraph.Taxes.Current = soGraph.Taxes.Current ?? soGraph.Taxes.Insert(soGraph.Taxes.Cache.CreateInstance() as SOTaxTran);
                                soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxID>(soGraph.Taxes.Current, row.Marketplace + "EC");
                                soGraph.Taxes.Update(soGraph.Taxes.Current);
                                #endregion

                                // Sales Order Save
                                soGraph.Save.Press();

                                #region Create PaymentRefund
                                var paymentExt = soGraph.GetExtension<CreatePaymentExt>();
                                paymentExt.SetDefaultValues(paymentExt.QuickPayment.Current, soGraph.Document.Current);
                                paymentExt.QuickPayment.Current.CuryRefundAmt = row.Net * -1;
                                paymentExt.QuickPayment.Current.CashAccountID = spCashAccount.CashAccountID;
                                paymentExt.QuickPayment.Current.ExtRefNbr = row.Checkout;
                                ARPaymentEntry paymentEntry = paymentExt.CreatePayment(paymentExt.QuickPayment.Current, soGraph.Document.Current, ARPaymentType.Refund);
                                paymentEntry.Document.Cache.SetValueExt<ARPayment.adjDate>(paymentEntry.Document.Current, row.TransactionDate);
                                paymentEntry.releaseFromHold.Press();
                                paymentEntry.release.Press();
                                paymentEntry.Save.Press();
                                #endregion

                                // Prepare Invoice
                                PrepareInvoiceAndOverrideTax(soGraph, soDoc);
                                #endregion
                                break;
                            case "CHARGEBACK WON":
                                #region TransactionType: CHARGEBACK WON
                                soGraph = PXGraph.CreateInstance<SOOrderEntry>();

                                #region Header
                                soDoc = soGraph.Document.Cache.CreateInstance() as SOOrder;
                                soDoc.OrderType = "IN";
                                soDoc.CustomerOrderNbr = row?.Checkout + "_" + ShopifyPublicFunction.GetUNIXTimestamp(row?.TransactionDate); ;
                                soDoc.OrderDate = row.TransactionDate;
                                soDoc.RequestDate = row.TransactionDate;
                                soDoc.CustomerID = ShopifyPublicFunction.GetMarketplaceCustomer(row.Marketplace);
                                soDoc.OrderDesc = $"Shopify Payment Gateway {row.TransactionType} {row.Checkout}";
                                #endregion

                                #region User-Defined
                                // UserDefined - ORDERTYPE
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERTYPE", $"Shopify Refund");
                                // UserDefined - MKTPLACE
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "MKTPLACE", row.Marketplace);
                                // UserDefined - ORDERAMT
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERAMT", Math.Abs(row.Net ?? 0));
                                // UserDefined - ORDTAAMT
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDTAXAMT", 0);
                                // UserDefined - TAXCOLLECT
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "TAXCOLLECT", 0);
                                #endregion

                                // Insert SOOrder
                                soGraph.Document.Insert(soDoc);

                                #region Set Currency
                                info = CurrencyInfoAttribute.SetDefaults<SOOrder.curyInfoID>(soGraph.Document.Cache, soGraph.Document.Current);
                                if (info != null)
                                    soGraph.Document.Cache.SetValueExt<SOOrder.curyID>(soGraph.Document.Current, info.CuryID);
                                #endregion

                                #region Address
                                soGraph_SP = PXGraph.CreateInstance<SOOrderEntry>();
                                soGraph_SP.Document.Current = oldShopifySOOrder;
                                if (soGraph_SP.Document.Current != null)
                                {
                                    // Setting Shipping_Address
                                    var soAddress = soGraph.Shipping_Address.Current;
                                    soGraph_SP.Shipping_Address.Current = soGraph_SP.Shipping_Address.Select();
                                    soAddress.OverrideAddress = true;
                                    soAddress.PostalCode = soGraph_SP.Shipping_Address.Current?.PostalCode;
                                    soAddress.CountryID = soGraph_SP.Shipping_Address.Current?.CountryID;
                                    soAddress.State = soGraph_SP.Shipping_Address.Current?.State;
                                    soAddress.City = soGraph_SP.Shipping_Address.Current?.City;
                                    soAddress.RevisionID = 1;
                                    // Setting Shipping_Contact
                                    var soContact = soGraph.Shipping_Contact.Current;
                                    soGraph_SP.Shipping_Contact.Current = soGraph_SP.Shipping_Contact.Select();
                                    soContact.OverrideContact = true;
                                    soContact.Email = soGraph_SP.Shipping_Contact.Current?.Email;
                                    soContact.RevisionID = 1;
                                }
                                #endregion

                                #region SOLine
                                // Amount
                                soTrans = soGraph.Transactions.Cache.CreateInstance() as SOLine;
                                soTrans.InventoryID = ShopifyPublicFunction.GetInvetoryitemID(soGraph, "CHARGEBACKWON");
                                soTrans.OrderQty = 1;
                                soTrans.CuryUnitPrice = row.Amount;
                                soGraph.Transactions.Insert(soTrans);
                                // Fee
                                if ((row?.Fee ?? 0) != 0)
                                {
                                    soTrans = soGraph.Transactions.Cache.CreateInstance() as SOLine;
                                    soTrans.InventoryID = ShopifyPublicFunction.GetInvetoryitemID(soGraph, "EC-COMMISSION");
                                    soTrans.OrderQty = 1;
                                    soTrans.CuryUnitPrice = row.Fee;
                                    soGraph.Transactions.Insert(soTrans);
                                }
                                #endregion

                                #region Update Tax
                                if (isTaxCalculate)
                                    soGraph.Document.Cache.SetValue<SOOrder.disableAutomaticTaxCalculation>(soGraph.Document.Current, false);
                                soGraph.Document.UpdateCurrent();

                                // Setting SO Tax
                                soGraph.Taxes.Current = soGraph.Taxes.Current ?? soGraph.Taxes.Insert(soGraph.Taxes.Cache.CreateInstance() as SOTaxTran);
                                soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxID>(soGraph.Taxes.Current, row.Marketplace + "EC");
                                soGraph.Taxes.Update(soGraph.Taxes.Current);
                                #endregion

                                // Sales Order Save
                                soGraph.Save.Press();

                                #region Create PaymentRefund
                                paymentExt = soGraph.GetExtension<CreatePaymentExt>();
                                paymentExt.SetDefaultValues(paymentExt.QuickPayment.Current, soGraph.Document.Current);
                                paymentExt.QuickPayment.Current.RefundAmt = row.Net * -1;
                                paymentExt.QuickPayment.Current.CashAccountID = spCashAccount.CashAccountID;
                                paymentExt.QuickPayment.Current.ExtRefNbr = row.Checkout;
                                paymentEntry = paymentExt.CreatePayment(paymentExt.QuickPayment.Current, soGraph.Document.Current, ARPaymentType.Payment);
                                paymentEntry.Document.Cache.SetValueExt<ARPayment.adjDate>(paymentEntry.Document.Current, row.TransactionDate);
                                paymentEntry.Save.Press();
                                paymentEntry.releaseFromHold.Press();
                                paymentEntry.release.Press();
                                #endregion

                                // Prepare Invoice
                                PrepareInvoiceAndOverrideTax(soGraph, soDoc);
                                #endregion
                                break;
                            case "SHOP_CASH_CREDIT":
                                #region TransactionType: SHOP_CASH_CREDIT
                                soGraph = PXGraph.CreateInstance<SOOrderEntry>();

                                #region Header
                                soDoc = soGraph.Document.Cache.CreateInstance() as SOOrder;
                                soDoc.OrderType = "IN";
                                soDoc.CustomerOrderNbr = row?.Checkout + "_" + ShopifyPublicFunction.GetUNIXTimestamp(row?.TransactionDate); ;
                                soDoc.OrderDate = row.TransactionDate;
                                soDoc.RequestDate = row.TransactionDate;
                                soDoc.CustomerID = ShopifyPublicFunction.GetMarketplaceCustomer(row.Marketplace);
                                soDoc.OrderDesc = $"Shopify Payment Gateway {row.TransactionType} {row.Checkout}";
                                #endregion

                                #region User-Defined
                                // UserDefined - ORDERTYPE
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERTYPE", $"Shopify Refund");
                                // UserDefined - MKTPLACE
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "MKTPLACE", row.Marketplace);
                                // UserDefined - ORDERAMT
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERAMT", Math.Abs(row.Net ?? 0));
                                // UserDefined - ORDTAAMT
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDTAXAMT", 0);
                                // UserDefined - TAXCOLLECT
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "TAXCOLLECT", 0);
                                #endregion

                                // Insert SOOrder
                                soGraph.Document.Insert(soDoc);

                                #region Set Currency
                                info = CurrencyInfoAttribute.SetDefaults<SOOrder.curyInfoID>(soGraph.Document.Cache, soGraph.Document.Current);
                                if (info != null)
                                    soGraph.Document.Cache.SetValueExt<SOOrder.curyID>(soGraph.Document.Current, info.CuryID);
                                #endregion

                                #region Address
                                soGraph_SP = PXGraph.CreateInstance<SOOrderEntry>();
                                soGraph_SP.Document.Current = oldShopifySOOrder;
                                if (soGraph_SP.Document.Current != null)
                                {
                                    // Setting Shipping_Address
                                    var soAddress = soGraph.Shipping_Address.Current;
                                    soGraph_SP.Shipping_Address.Current = soGraph_SP.Shipping_Address.Select();
                                    soAddress.OverrideAddress = true;
                                    soAddress.PostalCode = soGraph_SP.Shipping_Address.Current?.PostalCode;
                                    soAddress.CountryID = soGraph_SP.Shipping_Address.Current?.CountryID;
                                    soAddress.State = soGraph_SP.Shipping_Address.Current?.State;
                                    soAddress.City = soGraph_SP.Shipping_Address.Current?.City;
                                    soAddress.RevisionID = 1;
                                    // Setting Shipping_Contact
                                    var soContact = soGraph.Shipping_Contact.Current;
                                    soGraph_SP.Shipping_Contact.Current = soGraph_SP.Shipping_Contact.Select();
                                    soContact.OverrideContact = true;
                                    soContact.Email = soGraph_SP.Shipping_Contact.Current?.Email;
                                    soContact.RevisionID = 1;
                                }
                                #endregion

                                #region SOLine
                                // Amount
                                soTrans = soGraph.Transactions.Cache.CreateInstance() as SOLine;
                                soTrans.InventoryID = ShopifyPublicFunction.GetInvetoryitemID(soGraph, "SHOP-CASH");
                                soTrans.OrderQty = 1;
                                soTrans.CuryUnitPrice = row.Amount;
                                soGraph.Transactions.Insert(soTrans);
                                // Fee
                                if ((row?.Fee ?? 0) != 0)
                                {
                                    soTrans = soGraph.Transactions.Cache.CreateInstance() as SOLine;
                                    soTrans.InventoryID = ShopifyPublicFunction.GetInvetoryitemID(soGraph, "SHOP-CASH");
                                    soTrans.OrderQty = 1;
                                    soTrans.CuryUnitPrice = row?.Fee * -1;
                                    soGraph.Transactions.Insert(soTrans);
                                }
                                #endregion

                                #region Update Tax
                                if (isTaxCalculate)
                                    soGraph.Document.Cache.SetValue<SOOrder.disableAutomaticTaxCalculation>(soGraph.Document.Current, false);
                                soGraph.Document.UpdateCurrent();

                                // Setting SO Tax
                                soGraph.Taxes.Current = soGraph.Taxes.Current ?? soGraph.Taxes.Insert(soGraph.Taxes.Cache.CreateInstance() as SOTaxTran);
                                soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxID>(soGraph.Taxes.Current, row.Marketplace + "EC");
                                soGraph.Taxes.Update(soGraph.Taxes.Current);
                                #endregion

                                // Sales Order Save
                                soGraph.Save.Press();

                                #region Create PaymentRefund
                                paymentExt = soGraph.GetExtension<CreatePaymentExt>();
                                paymentExt.SetDefaultValues(paymentExt.QuickPayment.Current, soGraph.Document.Current);
                                paymentExt.QuickPayment.Current.RefundAmt = row.Net * -1;
                                paymentExt.QuickPayment.Current.CashAccountID = spCashAccount.CashAccountID;
                                paymentExt.QuickPayment.Current.ExtRefNbr = row.Checkout;
                                paymentEntry = paymentExt.CreatePayment(paymentExt.QuickPayment.Current, soGraph.Document.Current, ARPaymentType.Payment);
                                paymentEntry.Document.Cache.SetValueExt<ARPayment.adjDate>(paymentEntry.Document.Current, row.TransactionDate);
                                paymentEntry.Save.Press();
                                paymentEntry.releaseFromHold.Press();
                                paymentEntry.release.Press();
                                #endregion

                                // Prepare Invoice
                                PrepareInvoiceAndOverrideTax(soGraph, soDoc);
                                #endregion
                                break;
                            case "SHOP_CASH_REFUND_DEBIT":
                                #region TransactionType: SHOP_CASH_CREDIT
                                soGraph = PXGraph.CreateInstance<SOOrderEntry>();

                                #region Header
                                soDoc = soGraph.Document.Cache.CreateInstance() as SOOrder;
                                soDoc.OrderType = "CM";
                                soDoc.CustomerOrderNbr = row?.Checkout + "_" + ShopifyPublicFunction.GetUNIXTimestamp(row?.TransactionDate); ;
                                soDoc.OrderDate = row.TransactionDate;
                                soDoc.RequestDate = row.TransactionDate;
                                soDoc.CustomerID = ShopifyPublicFunction.GetMarketplaceCustomer(row.Marketplace);
                                soDoc.OrderDesc = $"Shopify Payment Gateway {row.TransactionType} {row.Checkout}";
                                #endregion

                                #region User-Defined
                                // UserDefined - ORDERTYPE
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERTYPE", $"Shopify Refund");
                                // UserDefined - MKTPLACE
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "MKTPLACE", row.Marketplace);
                                // UserDefined - ORDERAMT
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERAMT", Math.Abs(row.Net ?? 0));
                                // UserDefined - ORDTAAMT
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDTAXAMT", 0);
                                // UserDefined - TAXCOLLECT
                                soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "TAXCOLLECT", 0);
                                #endregion

                                // Insert SOOrder
                                soGraph.Document.Insert(soDoc);

                                #region Set Currency
                                info = CurrencyInfoAttribute.SetDefaults<SOOrder.curyInfoID>(soGraph.Document.Cache, soGraph.Document.Current);
                                if (info != null)
                                    soGraph.Document.Cache.SetValueExt<SOOrder.curyID>(soGraph.Document.Current, info.CuryID);
                                #endregion

                                #region Address
                                soGraph_SP = PXGraph.CreateInstance<SOOrderEntry>();
                                soGraph_SP.Document.Current = oldShopifySOOrder;
                                if (soGraph_SP.Document.Current != null)
                                {
                                    // Setting Shipping_Address
                                    var soAddress = soGraph.Shipping_Address.Current;
                                    soGraph_SP.Shipping_Address.Current = soGraph_SP.Shipping_Address.Select();
                                    soAddress.OverrideAddress = true;
                                    soAddress.PostalCode = soGraph_SP.Shipping_Address.Current?.PostalCode;
                                    soAddress.CountryID = soGraph_SP.Shipping_Address.Current?.CountryID;
                                    soAddress.State = soGraph_SP.Shipping_Address.Current?.State;
                                    soAddress.City = soGraph_SP.Shipping_Address.Current?.City;
                                    soAddress.RevisionID = 1;
                                    // Setting Shipping_Contact
                                    var soContact = soGraph.Shipping_Contact.Current;
                                    soGraph_SP.Shipping_Contact.Current = soGraph_SP.Shipping_Contact.Select();
                                    soContact.OverrideContact = true;
                                    soContact.Email = soGraph_SP.Shipping_Contact.Current?.Email;
                                    soContact.RevisionID = 1;
                                }
                                #endregion

                                #region SOLine
                                // Amount
                                soTrans = soGraph.Transactions.Cache.CreateInstance() as SOLine;
                                soTrans.InventoryID = ShopifyPublicFunction.GetInvetoryitemID(soGraph, "SHOP-CASH");
                                soTrans.OrderQty = 1;
                                soTrans.CuryUnitPrice = row?.Amount * -1;
                                soGraph.Transactions.Insert(soTrans);
                                // Fee
                                if ((row?.Fee ?? 0) != 0)
                                {
                                    soTrans = soGraph.Transactions.Cache.CreateInstance() as SOLine;
                                    soTrans.InventoryID = ShopifyPublicFunction.GetInvetoryitemID(soGraph, "SHOP-CASH");
                                    soTrans.OrderQty = 1;
                                    soTrans.CuryUnitPrice = row?.Fee;
                                    soGraph.Transactions.Insert(soTrans);
                                }
                                #endregion

                                #region Update Tax
                                if (isTaxCalculate)
                                    soGraph.Document.Cache.SetValue<SOOrder.disableAutomaticTaxCalculation>(soGraph.Document.Current, false);
                                soGraph.Document.UpdateCurrent();

                                // Setting SO Tax
                                soGraph.Taxes.Current = soGraph.Taxes.Current ?? soGraph.Taxes.Insert(soGraph.Taxes.Cache.CreateInstance() as SOTaxTran);
                                soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxID>(soGraph.Taxes.Current, row.Marketplace + "EC");
                                soGraph.Taxes.Update(soGraph.Taxes.Current);
                                #endregion

                                // Sales Order Save
                                soGraph.Save.Press();

                                #region Create PaymentRefund
                                paymentExt = soGraph.GetExtension<CreatePaymentExt>();
                                paymentExt.SetDefaultValues(paymentExt.QuickPayment.Current, soGraph.Document.Current);
                                paymentExt.QuickPayment.Current.RefundAmt = row.Net * -1;
                                paymentExt.QuickPayment.Current.CashAccountID = spCashAccount.CashAccountID;
                                paymentExt.QuickPayment.Current.ExtRefNbr = row.Checkout;
                                paymentEntry = paymentExt.CreatePayment(paymentExt.QuickPayment.Current, soGraph.Document.Current, ARPaymentType.Refund);
                                paymentEntry.Document.Cache.SetValueExt<ARPayment.adjDate>(paymentEntry.Document.Current, row.TransactionDate);
                                paymentEntry.Save.Press();
                                paymentEntry.releaseFromHold.Press();
                                paymentEntry.release.Press();
                                #endregion

                                // Prepare Invoice
                                PrepareInvoiceAndOverrideTax(soGraph, soDoc);
                                #endregion
                                break;
                            default:
                                break;
                        }
                        sc.Complete();
                    }
                }
                catch (PXOuterException ex)
                {
                    row.ErrorMessage = ex.InnerMessages[0];
                }
                catch (Exception ex)
                {
                    row.ErrorMessage = ex.Message;
                }
                finally
                {
                    row.IsProcessed = string.IsNullOrEmpty(row.ErrorMessage);
                    if (!row.IsProcessed.Value)
                        PXProcessing.SetError(row.ErrorMessage);
                    baseGraph.SettlementTransaction.Update(row);
                }
                baseGraph.Actions.PressSave();
            }
        }

        /// <summary> Sales Order Prepare Invoice and Override Tax </summary>
        public virtual void PrepareInvoiceAndOverrideTax(SOOrderEntry soGraph, SOOrder soDoc)
        {
            // Prepare Invoice
            try
            {
                soGraph.SelectTimeStamp();
                using (new PXTimeStampScope(soGraph.TimeStamp))
                {
                    soGraph.Document.Current.tstamp = soGraph.TimeStamp;
                    soGraph.releaseFromHold.Press();
                    soGraph.prepareInvoice.Press();
                }
            }
            // Prepare Invoice Success
            catch (PXRedirectRequiredException ex)
            {
                #region Override Invoice Tax
                // Invoice Graph
                SOInvoiceEntry invoiceGraph = ex.Graph as SOInvoiceEntry;
                // update docDate
                invoiceGraph.Document.SetValueExt<ARInvoice.docDate>(invoiceGraph.Document.Current, soDoc.RequestDate);
                invoiceGraph.Document.UpdateCurrent();
                // Save
                invoiceGraph.Save.Press();
                // Release Invoice
                invoiceGraph.releaseFromHold.Press();
                invoiceGraph.releaseFromCreditHold.Press();
                invoiceGraph.release.Press();
                #endregion
            }
        }

        #endregion

        public bool PrepareImportRow(string viewName, IDictionary keys, IDictionary values)
        {
            try
            {
                decimal _amount;
                decimal _presentmentAmount;
                // If type = ‘shop_cash_credit’ or ‘shop_cash_refund_debit’, then "Presentment Amount " could be null
                if (values["TransactionType"]?.ToString()?.ToUpper() == "SHOP_CASH_CREDIT" || values["TransactionType"]?.ToString()?.ToUpper() == "SHOP_CASH_REFUND_DEBIT")
                    return true;
                if (decimal.TryParse(values["Amount"].ToString(), out _amount) && decimal.TryParse(values["PresentmentAmount"].ToString(), out _presentmentAmount))
                {
                    var calculateResult = Math.Abs(_presentmentAmount / _amount);
                    if (values["Currency"].ToString() != values["PresentmentCurrency"].ToString())
                    {
                        values["Amount"] = _amount * calculateResult;
                        values["Fee"] = decimal.Parse(values["Fee"]?.ToString() ?? "0") * calculateResult;
                        values["Net"] = decimal.Parse(values["Net"].ToString() ?? "0") * calculateResult;
                    }
                }
                else
                    throw new Exception("Calculate Amount/Fee/Net Failed");
            }
            catch (Exception ex)
            {
                throw new Exception("Calculate Amount / Fee / Net Failed");
            }
            return true;
        }

        public void PrepareItems(string viewName, IEnumerable items)
        {
            throw new NotImplementedException();
        }

        public bool RowImported(string viewName, object row, object oldRow)
        {
            return true;
        }

        public bool RowImporting(string viewName, object row)
        {
            return true;
        }
    }
}

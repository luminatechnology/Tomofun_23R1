﻿using LUMTomofunCustomization.DAC;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CA;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.SO.GraphExtensions.SOOrderEntryExt;
using PX.Objects.AR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace LumTomofunCustomization.LUMLibrary
{
    public static class AmazonPublicFunction
    {
        #region Static Data Reception
        /// <summary> 轉換Amazon時間格式 </summary>
        public static DateTime CalculateAmazonDateTime(int? amazonDate)
            => DateTime.FromOADate(((amazonDate.Value + 8 * 3600) / 86400 + 70 * 365 + 19));

        /// <summary> Get Amazon Marketplace preference </summary>
        public static LUMMarketplacePreference GetMarketplacePreference(string marketplace)
            => SelectFrom<LUMMarketplacePreference>
               .Where<LUMMarketplacePreference.marketplace.IsEqual<P.AsString>>
               .View.Select(new PXGraph(), marketplace).TopFirst;

        /// <summary> 取Marketplace 對應 Customer ID </summary>
        public static int? GetMarketplaceCustomer(string marketPlace)
            => SelectFrom<LUMMarketplacePreference>
               .Where<LUMMarketplacePreference.marketplace.IsEqual<P.AsString>>
               .View.Select(new PXGraph(), marketPlace).TopFirst?.BAccountID;

        /// <summary> 取Marketplace 對應 Tax Calculation </summary>
        public static bool GetMarketplaceTaxCalculation(string marketPlace)
            => SelectFrom<LUMMarketplacePreference>
               .Where<LUMMarketplacePreference.marketplace.IsEqual<P.AsString>>
               .View.Select(new PXGraph(), marketPlace).TopFirst?.IsTaxCalculation ?? false;

        /// <summary> 取Fee 對應 Non-Stock item ID </summary>
        public static int? GetFeeNonStockItem(string fee)
            => SelectFrom<LUMMarketplaceFeePreference>
               .Where<LUMMarketplaceFeePreference.fee.IsEqual<P.AsString>>
               .View.Select(new PXGraph(), fee).TopFirst?.InventoryID;

        /// <summary> 取Inventory Item ID </summary>
        public static int? GetInvetoryitemID(PXGraph graph, string sku)
            => InventoryItem.UK.Find(graph, sku)?.InventoryID ??
               SelectFrom<INItemXRef>
               .Where<INItemXRef.alternateID.IsEqual<P.AsString>>
               .View.SelectSingleBound(graph, null, sku).TopFirst?.InventoryID;

        /// <summary> 取Inventory Item ID according to Country Code </summary>
        public static int? GetInvetoryitemID(PXGraph graph, string sku, string countryCode)
        {
            if (countryCode?.ToUpper() == "HK")
                return SelectFrom<INItemXRef>
                       .Where<INItemXRef.alternateID.IsEqual<P.AsString>>
                       .View.SelectSingleBound(graph, null, sku).TopFirst?.InventoryID ??
                       InventoryItem.UK.Find(graph, sku)?.InventoryID;
            else
                return InventoryItem.UK.Find(graph, sku)?.InventoryID ??
                       SelectFrom<INItemXRef>
                       .Where<INItemXRef.alternateID.IsEqual<P.AsString>>
                       .View.SelectSingleBound(graph, null, sku).TopFirst?.InventoryID;
        }

        /// <summary> 取Location ID By CD </summary>
        public static int? GetLocationID(int? siteid)
            => SelectFrom<INLocation>
               .Where<INLocation.siteID.IsEqual<P.AsInt>
                 .And<INLocation.locationCD.IsEqual<P.AsString>>>
               .View.SelectSingleBound(new PXGraph(), null, siteid, "RMA").TopFirst?.LocationID;
        #endregion

        /// <summary> 世界各國時區轉換 </summary>
        public static DateTime? DatetimeParseWithCulture(string cultureName, string sourceDatetime)
        {
            // a.m/p.m.處理
            sourceDatetime = sourceDatetime.Contains("a.m.") ? sourceDatetime.Replace("a.m.", "am") : sourceDatetime.Replace("p.m.", "pm");
            // 處理Sept問題
            sourceDatetime = sourceDatetime.ToLower().Replace("sept", "sep");
            // 處理日期歐洲英文月份轉換問題(文字ex:ago)
            var AbbreviatedMonthNames = CultureInfo.GetCultureInfo(cultureName).DateTimeFormat.AbbreviatedMonthNames;
            for (int i = 0; i < AbbreviatedMonthNames.Length - 1; i++)
            {
                if (sourceDatetime.Contains(AbbreviatedMonthNames[i].Replace(".", "")))
                {
                    sourceDatetime = sourceDatetime.Replace(AbbreviatedMonthNames[i].Replace(".", ""), AbbreviatedMonthNames[i]);

                    // Since the Italian "March" abbreviated month name will have conversion problems in the .Net framework, simply change all abbreviations to the genitive case.
                    if (cultureName.Contains("IT"))
                    {
                        sourceDatetime = sourceDatetime.Replace(AbbreviatedMonthNames[i], CultureInfo.GetCultureInfo(cultureName).DateTimeFormat.MonthGenitiveNames[i]);
                    }
                }
            }
            // 處理"Timezone"文字
            if (sourceDatetime.LastIndexOf(" ") != -1)
                sourceDatetime = sourceDatetime.Substring(0, sourceDatetime.LastIndexOf(" "));

            return DateTime.Parse(sourceDatetime, CultureInfo.GetCultureInfo(cultureName?.ToUpper()));
        }

        /// <summary> 世界各國貨幣轉換 </summary>
        public static decimal? CurrencyConvertWithCulture(string culturename, string sourceValue)
        {
            // 處理空白問題
            sourceValue = sourceValue.Replace(" ", "");
            // 處理全形符號問題
            sourceValue = sourceValue.Replace("−", "-");
            return decimal.Parse(sourceValue, NumberStyles.Currency, CultureInfo.GetCultureInfo(culturename).NumberFormat);
        }

        /// <summary> 世界各國OrderType轉換 </summary>
        public static string AmazonOrderTypeTreanslate(string marketplaceName, string sourceOrderType)
        {
            Dictionary<string, string> ordertypeDic = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(sourceOrderType))
                return sourceOrderType;
            switch (marketplaceName)
            {
                case "DE":
                    ordertypeDic = new Dictionary<string, string>()
                    {
                        { "Anpassung","Adjustment"},
                        { "Bestellung","Order"},
                        { "Bestellung_Wiedereinzug","Order_Retrocharge"},
                        { "Blitzangebotsgebühr","Lightning Deal Fee"},
                        { "Erstattung","Refund"},
                        { "Erstattung durch Rückbuchung","Chargeback Refund"},
                        { "Erstattung_Wiedereinzug","Refund_Retrocharge"},
                        { "Servicegebühr","Service Fee"},
                        { "Übertrag","Transfer"},
                        { "Verbindlichkeit","Debt"},
                        { "Versand durch Amazon Lagergebühr","FBA Inventory Fee"},
                    };
                    break;
                case "ES":
                    ordertypeDic = new Dictionary<string, string>()
                    {
                        { "Ajuste","Adjustment"},
                        { "Pedido","Order"},
                        { "Reembolso","Refund"},
                        { "Saldo descubierto","Debt"},
                        { "Tarifa de Oferta flash","Lightning Deal Fee"},
                        { "Tarifa de prestación de servicio","Service Fee"},
                        { "Tarifas de inventario de Logística de Amazon","FBA Inventory Fee"},
                        { "Transferir","Transfer"}
                    };
                    break;
                case "FR":
                    ordertypeDic = new Dictionary<string, string>()
                    {
                        { "Ajustement","Adjustment"},
                        { "Commande","Order"},
                        { "Frais de service","Service Fee"},
                        { "Frais de stock Expédié par Amazon","FBA Inventory Fee"},
                        { "Remboursement","Refund"},
                        { "Solde négatif","Debt"},
                        { "Tarif de la Vente Flash","Lightning Deal Fee"},
                        { "Transfert","Transfer"}
                    };
                    break;
                case "IT":
                    ordertypeDic = new Dictionary<string, string>()
                    {
                        { "Commissione di servizio","Service Fee"},
                        { "Costo di stoccaggio Logistica di Amazon","FBA Inventory Fee"},
                        { "Modifica","Adjustment"},
                        { "Ordine","Order"},
                        { "Rimborso","Refund"},
                        { "Saldo negativo","Debt"},
                        //{"Tariffa dell’Offerta", "Lightning Deal Fee"},
                        { "Tariffa dell’Offerta Lampo","Lightning Deal Fee"},
                        //{ "Tariffa dell’Offerta Lampo","Lightning Offer Rate"},
                        { "Trasferimento","Transfer"}
                    };
                    break;
                case "JP":
                    ordertypeDic = new Dictionary<string, string>()
                    {
                        { "FBA 在庫関連の手数料","FBA Inventory Fee"},
                        { "注文","Order"},
                        { "注文外料金","Service Fee"},
                        { "返金","Refund"},
                        { "振込み","Transfer"},
                        { "数量限定タイムセール手数料","Lightning Deal Fee"},
                        { "調整","Adjustment"}
                    };
                    break;
                case "MX":
                    ordertypeDic = new Dictionary<string, string>()
                    {
                        { "Ajuste","Adjustment"},
                        { "Pedido","Order"},
                        { "Reembolso","Refund"},
                        { "Tarifa de inventario FBA","FBA Inventory Fee"},
                        { "Tarifa de servicio","Service Fee"},
                        { "Trasferir","Transfer"},
                        { "Deuda","Debt"}
                    };
                    break;
                case "NL":
                    ordertypeDic = new Dictionary<string, string>()
                    {
                        { "Aanpassing","Adjustment"},
                        { "Bestelling","Order"},
                        { "Overboeking","Transfer"},
                        { "Servicekosten","Service Fee"},
                        { "Terugbetaling","Refund"},
                        { "Schuld","Debt"}
                    };
                    break;
                case "SE":
                    ordertypeDic = new Dictionary<string, string>()
                    {
                        { "Order","Order"},
                        { "Överföring","Transfer"},
                        { "Serviceavgift","Service Fee"},
                        { "Återbetalning","Refund"},
                        { "Skuld","Debt"},
                        { "Lageravgift för FBA","FBA Inventory Fee"}
                    };
                    break;
                case "BE":
                    ordertypeDic = new Dictionary<string, string>()
                    {
                        { "Commande","Order"},
                        { "Transférer","Transfer"}
                    };
                    break;
                default:
                    return sourceOrderType;
            }
            return ordertypeDic.ContainsKey(sourceOrderType) ? ordertypeDic[sourceOrderType] : sourceOrderType;
        }
    }

    /// <summary> Amazon Payment 產生Payment/Refund... Main method</summary>
    public static class AmazonToAcumaticaCore<T> where T : class
    {
        private static Object thisLock = new Object();

        /// <summary> Create Amazon Payment </summary>
        public static void CreatePayment(T selectedItem, string _marketplace, PXGraph baseGraph)
        {
            // 取Marketplace Preference Data
            var amazonData = JsonConvert.DeserializeObject<AmazonExcelEntity>(JsonConvert.SerializeObject(selectedItem));
            var isTaxCalculate = AmazonPublicFunction.GetMarketplaceTaxCalculation(_marketplace);
            //var amzTotalTax = amzGroupOrderData.Where(x => x.AmountDescription == "Tax" || x.AmountDescription == "ShippingTax" || x.AmountDescription == "TaxDiscount" || x.AmountType == "ItemWithheldTax").Sum(x => (x.Amount ?? 0) * -1);
            using (new PXCommandScope(3000))
            {
                var paycheck = $"{amazonData?.Api_date?.ToString("yyyyMMddHHmmss")}_{amazonData?.Api_trantype}_{amazonData?.Api_orderid}_{amazonData.Api_sku}_{amazonData?.Api_total?.ToString("0.0000")}";

                switch (amazonData.Api_trantype?.ToUpper())
                {
                    case "REFUND":
                        #region Transaction Type: Refund

                        var soGraph = PXGraph.CreateInstance<SOOrderEntry>();

                        #region Header
                        var soDoc = soGraph.Document.Cache.CreateInstance() as SOOrder;
                        soDoc.OrderType = "CM";
                        soDoc.CustomerOrderNbr = amazonData?.Api_orderid;
                        soDoc.OrderDate = amazonData?.Api_date;
                        soDoc.RequestDate = amazonData?.Api_date;
                        soDoc.CustomerID = AmazonPublicFunction.GetMarketplaceCustomer(_marketplace);
                        soDoc.OrderDesc = $"Amazon ({amazonData?.Api_trantype}) {amazonData?.Api_orderid}";
                        #endregion

                        #region User-Defined
                        // UserDefined - ORDERTYPE
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERTYPE", $"Amazon {amazonData?.Api_trantype}");
                        // UserDefined - MKTPLACE
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "MKTPLACE", _marketplace);
                        // UserDefined - ORDERAMT
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERAMT", Math.Abs(amazonData?.Api_total ?? 0));
                        // UserDefined - PAYCHECK
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "PAYCHECK", paycheck);
                        #endregion

                        // Insert SOOrder
                        soGraph.Document.Insert(soDoc);

                        #region Set Currency
                        CurrencyInfo info = CurrencyInfoAttribute.SetDefaults<SOOrder.curyInfoID>(soGraph.Document.Cache, soGraph.Document.Current);
                        if (info != null)
                            soGraph.Document.Cache.SetValueExt<SOOrder.curyID>(soGraph.Document.Current, info.CuryID);
                        #endregion

                        #region Address/Contact
                        var FAInfomation = SelectFrom<SOOrder>
                                           .InnerJoin<SOShippingContact>.On<SOOrder.shipContactID.IsEqual<SOShippingContact.contactID>>
                                           .InnerJoin<SOShippingAddress>.On<SOOrder.shipAddressID.IsEqual<SOShippingAddress.addressID>>
                                           .Where<SOOrder.orderType.IsEqual<P.AsString>
                                             .And<SOOrder.customerOrderNbr.IsEqual<P.AsString>>>
                                           .View.SelectSingleBound(soGraph, null, "FA", amazonData?.Api_orderid);


                        if (FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault() != null)
                            SOShippingAddressAttribute.Copy(soGraph.Shipping_Address.Current, FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault());
                        if (FAInfomation.RowCast<SOShippingContact>().FirstOrDefault() != null)
                            SOShippingContactAttribute.CopyContact(soGraph.Shipping_Contact.Current, FAInfomation.RowCast<SOShippingContact>().FirstOrDefault());
                        #endregion

                        var customerDefWarehouse = SelectFrom<Location>
                                                   .Where<Location.bAccountID.IsEqual<P.AsInt>>
                                                   .View.SelectSingleBound(baseGraph, null, soDoc.CustomerID)
                                                   .TopFirst?.CSiteID;
                        #region SOLine
                        // Refund
                        if (amazonData?.Api_productsales != 0)
                            soGraph.Transactions.Insert(
                                CreateSOLineObject(soGraph,
                                                   inventoryCD: _marketplace == "TW" ? "REFUND-TW" : "REFUND",
                                                   desc: null,
                                                   unitPrice: amazonData?.Api_productsales * -1,
                                                   discountAmt: amazonData?.Api_promotion));

                        #region TAX
                        // API_SHIPPING_TAX
                        if (amazonData?.Api_shippingtax != 0)
                            soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: $"EC-WHTAX-{_marketplace}",
                                               desc: "API-SHIPPING-TAX",
                                               unitPrice: amazonData?.Api_shippingtax * -1));

                        // API_PRODUCT_TAX
                        if (amazonData?.Api_producttax != 0)
                            soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: $"EC-WHTAX-{_marketplace}",
                                               desc: "API-PRODUCT-TAX",
                                               unitPrice: amazonData?.Api_producttax * -1));

                        // API_GIFTWRAP_TAX
                        if (amazonData?.Api_giftwraptax != 0)
                            soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: $"EC-WHTAX-{_marketplace}",
                                               desc: "API-GIFTWRAP-TAX",
                                               unitPrice: amazonData?.Api_giftwraptax * -1));

                        // API_TAX ON REGULATORY FEE
                        if (amazonData?.Api_taxonregulatoryfee != 0)
                            soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: $"EC-WHTAX-{_marketplace}",
                                               desc: "API_TAX ON REGULATORY FEE",
                                               unitPrice: amazonData?.Api_taxonregulatoryfee * -1));

                        // API_PROMOTION_TAX
                        if (amazonData?.Api_promotiontax != 0)
                            soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: $"EC-WHTAX-{_marketplace}",
                                               desc: "API_PROMOTION_TAX",
                                               unitPrice: amazonData?.Api_promotiontax * -1));

                        // API_WH_TAX
                        if (amazonData?.Api_whtax != 0)
                            soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: $"EC-WHTAX-{_marketplace}",
                                               desc: "API_WH_TAX",
                                               unitPrice: amazonData?.Api_whtax * -1));
                        #endregion

                        #region ITEM
                        // API_SHIPPING
                        if (amazonData?.Api_shipping != 0)
                            soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: $"API-SHIPPING",
                                               desc: null,
                                               unitPrice: amazonData?.Api_shipping * -1));

                        // API_GIFTWRAP
                        if (amazonData?.Api_giftwrap != 0)
                            soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: $"API-GIFTWRAP",
                                               desc: null,
                                               unitPrice: amazonData?.Api_giftwrap * -1));

                        // API_REGULATORY FEE
                        if (amazonData?.Api_regulatoryfee != 0)
                            soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: $"API-REGULATORY-FEE",
                                               desc: null,
                                               unitPrice: amazonData?.Api_regulatoryfee * -1));

                        // API_SELLING_FEE
                        if (amazonData?.Api_sellingfee != 0)
                            soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: $"API-SELLING-FEE",
                                               desc: null,
                                               unitPrice: amazonData?.Api_sellingfee * -1));

                        // API_FBA_FEE
                        if (amazonData?.Api_fbafee != 0)
                            soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: $"API-FBA-FEE",
                                               desc: null,
                                               unitPrice: amazonData?.Api_fbafee * -1));

                        // API_OTHER_TRAN_FEE
                        if (amazonData?.Api_othertranfee != 0)
                            soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: $"API-OTHER-TRAN-FEE",
                                               desc: null,
                                               unitPrice: amazonData?.Api_othertranfee * -1));

                        // API_OTHER_FEE
                        if (amazonData?.Api_otherfee != 0)
                            soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: $"API-OTHER-FEE",
                                               desc: null,
                                               unitPrice: amazonData?.Api_otherfee * -1));

                        // API_POINTS
                        if (amazonData?.Api_points != 0)
                            soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: $"API-POINTS",
                                               desc: null,
                                               unitPrice: amazonData?.Api_points * -1));
                        #endregion

                        #endregion

                        #region Update Tax
                        if (isTaxCalculate)
                            soGraph.Document.Cache.SetValue<SOOrder.disableAutomaticTaxCalculation>(soGraph.Document.Current, false);
                        soGraph.Document.UpdateCurrent();

                        // Setting SO Tax
                        soGraph.Taxes.Current = soGraph.Taxes.Current ?? soGraph.Taxes.Insert(soGraph.Taxes.Cache.CreateInstance() as SOTaxTran);
                        soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxID>(soGraph.Taxes.Current, isTaxCalculate ? $"{_marketplace}ECZ" : $"{_marketplace}EC");
                        #endregion

                        lock (thisLock)
                        {
                            #region Valid Payment check

                            ValidPaymentCheck(paycheck, GetCustomerDefaultPaymentMethodInfo(soDoc.CustomerID, baseGraph)?.CashAccountID, "Order", baseGraph);

                            #endregion

                            // Sales Order Save
                            ValidAndSaveOrder(soGraph, amazonData);
                        }

                        #region Create PaymentRefund
                        var paymentExt = soGraph.GetExtension<CreatePaymentExt>();
                        ARPaymentEntry paymentEntry = null;
                        if (amazonData?.Api_total != 0)
                        {
                            paymentExt = soGraph.GetExtension<CreatePaymentExt>();
                            paymentExt.SetDefaultValues(paymentExt.QuickPayment.Current, soGraph.Document.Current);
                            paymentExt.QuickPayment.Current.ExtRefNbr = amazonData?.Api_settlementid;
                            paymentEntry = paymentExt.CreatePayment(paymentExt.QuickPayment.Current, soGraph.Document.Current, ARPaymentType.Refund);
                            paymentEntry.Document.Cache.SetValueExt<ARPayment.adjDate>(paymentEntry.Document.Current, amazonData?.Api_date);
                            paymentEntry.Save.Press();
                            paymentEntry.releaseFromHold.Press();
                            lock (thisLock)
                                paymentEntry.release.Press();
                        }
                        #endregion

                        // Prepare Invoice
                        PrepareInvoiceAndOverrideTax(soGraph, soDoc);
                        #endregion
                        break;
                    case "ORDER":
                        #region Transaction Type: Order
                        #region Create Payment (1.2.2.1)
                        if (!(amazonData?.Api_orderid?.ToUpper().StartsWith("S") ?? false))
                        {
                            var arGraph = PXGraph.CreateInstance<ARPaymentEntry>();

                            #region Header(Document)
                            var arDoc = arGraph.Document.Cache.CreateInstance() as ARPayment;
                            arDoc.DocType = "PMT";
                            arDoc.AdjDate = amazonData?.Api_date.Value.Date;
                            arDoc.ExtRefNbr = amazonData?.Api_settlementid;
                            arDoc.CustomerID = AmazonPublicFunction.GetMarketplaceCustomer(_marketplace);
                            arDoc.DocDesc = $"Amazon ({amazonData?.Api_trantype}) {amazonData?.Api_orderid}";
                            arDoc.DepositDate = amazonData?.Api_date.Value.Date;
                            if (arDoc.DepositDate == null)
                                throw new Exception($"can not find Deposit Date({amazonData?.Api_settlementid})");

                            #region User-Defiend
                            // UserDefined - ECNETPAY
                            arGraph.Document.Cache.SetValueExt(arDoc, PX.Objects.CS.Messages.Attribute + "ECNETPAY", amazonData?.Api_total);
                            // UserDefined - PAYCHECK
                            arGraph.Document.Cache.SetValueExt(arDoc, PX.Objects.CS.Messages.Attribute + "PAYCHECK", paycheck);
                            #endregion

                            arGraph.Document.Insert(arDoc);
                            #endregion

                            #region Valid Payment check

                            ValidPaymentCheck(paycheck, arGraph.Document.Current?.CashAccountID, "Payment", baseGraph);

                            #endregion

                            #region Adjustments
                            var mapInvoice = SelectFrom<ARInvoice>
                                                 .InnerJoin<ARTran>.On<ARInvoice.docType.IsEqual<ARTran.tranType>
                                                       .And<ARInvoice.refNbr.IsEqual<ARTran.refNbr>>>
                                                 .InnerJoin<SOOrder>.On<ARTran.sOOrderNbr.IsEqual<SOOrder.orderNbr>>
                                                 .Where<ARInvoice.invoiceNbr.IsEqual<P.AsString>
                                                   .And<SOOrder.orderType.IsEqual<P.AsString>>>
                                                 .OrderBy<Desc<ARInvoice.createdDateTime>>
                                                 .View.SelectSingleBound(baseGraph, null, amazonData?.Api_orderid, "FA").TopFirst;
                            if (mapInvoice == null)
                                throw new Exception($"Can not Find Invoice (OrderID: {amazonData?.Api_orderid})");
                            var adjTrans = arGraph.Adjustments.Cache.CreateInstance() as ARAdjust;
                            adjTrans.AdjdDocType = "INV";
                            adjTrans.AdjdRefNbr = mapInvoice?.RefNbr;
                            adjTrans.CuryAdjgAmt = amazonData?.Api_productsales + amazonData?.Api_producttax + amazonData?.Api_shipping + amazonData?.Api_shippingtax + amazonData?.Api_giftwrap + amazonData?.Api_giftwraptax + amazonData?.Api_promotion + amazonData?.Api_promotiontax;
                            arGraph.Adjustments.Insert(adjTrans);
                            #endregion

                            #region CHARGS
                            // FBAREGULAT
                            if (amazonData?.Api_regulatoryfee != 0)
                                arGraph.PaymentCharges.Insert(CreatePaymentCHARGESObject(arGraph, "FBAREGULAT", amazonData?.Api_regulatoryfee * -1));

                            // WHTAX + Marketplace
                            if (amazonData?.Api_taxonregulatoryfee != 0)
                                arGraph.PaymentCharges.Insert(CreatePaymentCHARGESObject(arGraph, $"WHTAX{_marketplace}", amazonData?.Api_taxonregulatoryfee * -1));

                            // WHTAX + Marketplace
                            if (amazonData?.Api_whtax != 0)
                                arGraph.PaymentCharges.Insert(CreatePaymentCHARGESObject(arGraph, $"WHTAX{_marketplace}", amazonData?.Api_whtax * -1));

                            // FBASELLING
                            if (amazonData?.Api_sellingfee != 0)
                                arGraph.PaymentCharges.Insert(CreatePaymentCHARGESObject(arGraph, "FBASELLING", amazonData?.Api_sellingfee * -1));

                            // FBAFEE
                            if ((amazonData?.Api_fbafee + amazonData?.Api_otherfee) != 0)
                                arGraph.PaymentCharges.Insert(CreatePaymentCHARGESObject(arGraph, "FBAFEE", (amazonData?.Api_fbafee + amazonData?.Api_otherfee) * -1));

                            // FBAOTHTRAN
                            if (amazonData?.Api_othertranfee != 0)
                                arGraph.PaymentCharges.Insert(CreatePaymentCHARGESObject(arGraph, "FBAOTHTRAN", amazonData?.Api_othertranfee * -1));

                            // FBAOTHER
                            //if (amazonData?.Api_otherfee != 0)
                            //    arGraph.PaymentCharges.Insert(CreatePaymentCHARGESObject(arGraph, "FBAOTHER", amazonData?.Api_otherfee * -1));

                            // FBAPOINTS
                            if (amazonData?.Api_points != 0)
                                arGraph.PaymentCharges.Insert(CreatePaymentCHARGESObject(arGraph, "FBAPOINT", amazonData?.Api_points * -1));
                            #endregion

                            // set payment amount to apply amount
                            arGraph.Document.SetValueExt<ARPayment.curyOrigDocAmt>(arGraph.Document.Current, arGraph.Document.Current.CuryApplAmt);
                            // Save Payment
                            arGraph.Actions.PressSave();
                            // Release Payment
                            arGraph.releaseFromHold.Press();
                            lock (thisLock)
                                arGraph.release.Press();
                        }
                        #endregion

                        #region Create MCF COD Refund Sales Order(Spec 1.2.2.3)
                        // Order ID starts with ‘S’ and API_COD_ITEMCHARGE < 0
                        else if ((amazonData?.Api_orderid?.ToUpper().StartsWith("S") ?? false) && amazonData?.Api_coditemcharge < 0)
                        {
                            soGraph = PXGraph.CreateInstance<SOOrderEntry>();

                            #region Header
                            soDoc = soGraph.Document.Cache.CreateInstance() as SOOrder;
                            soDoc.OrderType = "CM";
                            soDoc = soGraph.Document.Cache.Insert(soDoc) as SOOrder;
                            soDoc.CustomerOrderNbr = amazonData?.Api_orderid;
                            soDoc.OrderDate = amazonData?.Api_date;
                            soDoc.RequestDate = amazonData?.Api_date;
                            soDoc.CustomerID = AmazonPublicFunction.GetMarketplaceCustomer(_marketplace);
                            soDoc.CustomerLocationID = SelectFrom<Location>.Where<Location.locationCD.IsEqual<P.AsString>>.View.Select(baseGraph, "COD")?.TopFirst?.LocationID;
                            soDoc.OrderDesc = $"Amazon ({amazonData?.Api_trantype}) {amazonData?.Api_orderid}";
                            #endregion

                            #region User-Defined
                            // UserDefined - ORDERTYPE
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERTYPE", $"Amazon MCF");
                            // UserDefined - MKTPLACE
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "MKTPLACE", _marketplace);
                            // UserDefined - ORDERAMT
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERAMT", Math.Abs(amazonData?.Api_total ?? 0));
                            // UserDefined - PAYCHECK
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "PAYCHECK", paycheck);
                            #endregion

                            // Insert SOOrder
                            soGraph.Document.Cache.Update(soDoc);

                            #region Set Currency
                            info = CurrencyInfoAttribute.SetDefaults<SOOrder.curyInfoID>(soGraph.Document.Cache, soGraph.Document.Current);
                            if (info != null)
                                soGraph.Document.Cache.SetValueExt<SOOrder.curyID>(soGraph.Document.Current, info.CuryID);
                            #endregion

                            #region Address/Contact
                            FAInfomation = SelectFrom<SOOrder>
                                               .InnerJoin<SOShippingContact>.On<SOOrder.shipContactID.IsEqual<SOShippingContact.contactID>>
                                               .InnerJoin<SOShippingAddress>.On<SOOrder.shipAddressID.IsEqual<SOShippingAddress.addressID>>
                                               .Where<SOOrder.orderType.IsEqual<P.AsString>
                                                 .And<SOOrder.customerOrderNbr.IsEqual<P.AsString>>>
                                               .View.SelectSingleBound(soGraph, null, "FA", amazonData?.Api_orderid);


                            if (FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault() != null)
                                SOShippingAddressAttribute.Copy(soGraph.Shipping_Address.Current, FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault());
                            if (FAInfomation.RowCast<SOShippingContact>().FirstOrDefault() != null)
                                SOShippingContactAttribute.CopyContact(soGraph.Shipping_Contact.Current, FAInfomation.RowCast<SOShippingContact>().FirstOrDefault());
                            #endregion

                            #region SOLine
                            // EC-SHIPPING
                            var _codFee = (Math.Abs(amazonData.Api_coditemcharge.Value) >= 0 && Math.Abs(amazonData.Api_coditemcharge.Value) < 30433) ? 432 :
                                          (Math.Abs(amazonData.Api_coditemcharge.Value) >= 30433 && Math.Abs(amazonData.Api_coditemcharge.Value) < 100649) ? 648 : 1080;
                            soGraph.Transactions.Insert(CreateSOLineObject(soGraph, inventoryCD: "EC-SHIPPING", desc: "COD Fee", unitPrice: _codFee, orderQty: 1, taxCategoryID: "NONTAXABLE"));
                            // CODREFUND
                            if (amazonData?.Api_total != 0)
                                soGraph.Transactions.Insert(CreateSOLineObject(soGraph, "CODREFUND", "COD Refund", Math.Abs(amazonData?.Api_total ?? 0) - _codFee));
                            #endregion

                            #region Update Tax
                            // Setting SO Tax
                            if (isTaxCalculate)
                                soGraph.Document.Cache.SetValue<SOOrder.disableAutomaticTaxCalculation>(soGraph.Document.Current, false);
                            soGraph.Document.UpdateCurrent();

                            // 只有JP有兩個Tax
                            if (soDoc?.TaxZoneID == "JPAMZCOD")
                                soGraph.Taxes.Insert(new SOTaxTran() { TaxID = isTaxCalculate ? $"{_marketplace}ECZ" : $"{_marketplace}EC" });
                            else
                            {
                                soGraph.Taxes.Current = soGraph.Taxes.Current ?? soGraph.Taxes.Insert(soGraph.Taxes.Cache.CreateInstance() as SOTaxTran);
                                soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxID>(soGraph.Taxes.Current, isTaxCalculate ? $"{_marketplace}ECZ" : $"{_marketplace}EC");
                            }
                            #endregion

                            lock (thisLock)
                            {
                                #region Valid Payment check

                                ValidPaymentCheck(paycheck, GetCustomerDefaultPaymentMethodInfo(soDoc.CustomerID, baseGraph)?.CashAccountID, "Order", baseGraph);

                                #endregion

                                // Sales Order Save
                                ValidAndSaveOrder(soGraph, amazonData);
                            }

                            #region Create PaymentRefund
                            if (amazonData?.Api_total != 0)
                            {
                                paymentExt = soGraph.GetExtension<CreatePaymentExt>();
                                paymentExt.SetDefaultValues(paymentExt.QuickPayment.Current, soGraph.Document.Current);
                                paymentExt.QuickPayment.Current.ExtRefNbr = amazonData?.Api_settlementid;
                                paymentEntry = paymentExt.CreatePayment(paymentExt.QuickPayment.Current, soGraph.Document.Current, ARPaymentType.Refund);
                                paymentEntry.Document.Cache.SetValueExt<ARPayment.adjDate>(paymentEntry.Document.Current, amazonData?.Api_date);
                                paymentEntry.Save.Press();
                                paymentEntry.releaseFromHold.Press();
                                lock (thisLock)
                                    paymentEntry.release.Press();
                            }
                            #endregion

                            // Prepare Invoice
                            PrepareInvoiceAndOverrideTax(soGraph, soDoc, false);
                        }
                        #endregion

                        #region 1.2.2.2	Create MCF COD Sales Order (Spec 1.2.2.2)
                        // Order ID starts with ‘S’ and [Amount Description] contain ‘CODItemCharge’ and CODItemCharge.Amount > 0
                        else if ((amazonData?.Api_orderid?.ToUpper().StartsWith("S") ?? false) && amazonData?.Api_coditemcharge > 0)
                        {
                            #region Create SO Type: IN
                            soGraph = PXGraph.CreateInstance<SOOrderEntry>();

                            #region Header
                            soDoc = soGraph.Document.Cache.CreateInstance() as SOOrder;
                            soDoc.OrderType = "IN";
                            soDoc.CustomerOrderNbr = amazonData?.Api_orderid;
                            soDoc.OrderDate = amazonData?.Api_date;
                            soDoc.RequestDate = amazonData?.Api_date;
                            soDoc.CustomerID = AmazonPublicFunction.GetMarketplaceCustomer(_marketplace);
                            soDoc.OrderDesc = $"Amazon ({amazonData?.Api_trantype}) {amazonData?.Api_orderid}";
                            #endregion

                            #region User-Defined
                            // UserDefined - ORDERTYPE
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERTYPE", $"Amazon MCF");
                            // UserDefined - MKTPLACE
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "MKTPLACE", _marketplace);
                            // UserDefined - ORDERAMT
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERAMT", Math.Abs(amazonData?.Api_total ?? 0));
                            // UserDefined - PAYCHECK
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "PAYCHECK", paycheck);
                            #endregion

                            // Insert SOOrder
                            soGraph.Document.Insert(soDoc);

                            #region Set Currency
                            info = CurrencyInfoAttribute.SetDefaults<SOOrder.curyInfoID>(soGraph.Document.Cache, soGraph.Document.Current);
                            if (info != null)
                                soGraph.Document.Cache.SetValueExt<SOOrder.curyID>(soGraph.Document.Current, info.CuryID);
                            #endregion

                            #region Address/Contact
                            FAInfomation = SelectFrom<SOOrder>
                                               .InnerJoin<SOShippingContact>.On<SOOrder.shipContactID.IsEqual<SOShippingContact.contactID>>
                                               .InnerJoin<SOShippingAddress>.On<SOOrder.shipAddressID.IsEqual<SOShippingAddress.addressID>>
                                               .Where<SOOrder.orderType.IsEqual<P.AsString>
                                                 .And<SOOrder.customerOrderNbr.IsEqual<P.AsString>>>
                                               .View.SelectSingleBound(soGraph, null, "FA", amazonData?.Api_orderid);


                            if (FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault() != null)
                                SOShippingAddressAttribute.Copy(soGraph.Shipping_Address.Current, FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault());
                            if (FAInfomation.RowCast<SOShippingContact>().FirstOrDefault() != null)
                                SOShippingContactAttribute.CopyContact(soGraph.Shipping_Contact.Current, FAInfomation.RowCast<SOShippingContact>().FirstOrDefault());
                            #endregion

                            #region SOLine

                            // COD_ITEMCHARGE
                            if (amazonData?.Api_coditemcharge != 0)
                                soGraph.Transactions.Insert(CreateSOLineObject(soGraph, "COD-REVENUE-JP", "COD_ITEMCHARGE", amazonData?.Api_coditemcharge));

                            // COD_ITEMCHARGE
                            if (amazonData?.Api_total - amazonData?.Api_coditemcharge > 0)
                                soGraph.Transactions.Insert(CreateSOLineObject(soGraph, "EC-SHIPPING", "COD Shipping Fee", amazonData?.Api_total - amazonData?.Api_coditemcharge));

                            #endregion

                            #region Update Tax
                            if (isTaxCalculate)
                                soGraph.Document.Cache.SetValue<SOOrder.disableAutomaticTaxCalculation>(soGraph.Document.Current, false);
                            soGraph.Document.UpdateCurrent();

                            // Setting SO Tax
                            soGraph.Taxes.Current = soGraph.Taxes.Current ?? soGraph.Taxes.Insert(soGraph.Taxes.Cache.CreateInstance() as SOTaxTran);
                            soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxID>(soGraph.Taxes.Current, isTaxCalculate ? $"{_marketplace}ECZ" : $"{_marketplace}EC");
                            #endregion

                            lock (thisLock)
                            {
                                #region Valid Payment check

                                ValidPaymentCheck(paycheck, GetCustomerDefaultPaymentMethodInfo(soDoc.CustomerID, baseGraph)?.CashAccountID, "Order", baseGraph);

                                #endregion

                                // Sales Order Save
                                ValidAndSaveOrder(soGraph, amazonData);
                            }

                            #region Create PaymentRefund
                            if (amazonData?.Api_total != 0)
                            {
                                paymentExt = soGraph.GetExtension<CreatePaymentExt>();
                                paymentExt.SetDefaultValues(paymentExt.QuickPayment.Current, soGraph.Document.Current);
                                paymentExt.QuickPayment.Current.ExtRefNbr = amazonData?.Api_settlementid;
                                paymentEntry = paymentExt.CreatePayment(paymentExt.QuickPayment.Current, soGraph.Document.Current, soDoc.OrderType == "CM" ? ARPaymentType.Refund : ARPaymentType.Payment);
                                paymentEntry.Document.Cache.SetValueExt<ARPayment.adjDate>(paymentEntry.Document.Current, amazonData?.Api_date);
                                paymentEntry.Save.Press();
                                paymentEntry.releaseFromHold.Press();
                                lock (thisLock)
                                    paymentEntry.release.Press();
                            }
                            #endregion

                            // Prepare Invoice
                            PrepareInvoiceAndOverrideTax(soGraph, soDoc);
                            #endregion

                            #region Create SO Type: CM
                            soGraph = PXGraph.CreateInstance<SOOrderEntry>();

                            #region Header
                            soDoc = soGraph.Document.Cache.CreateInstance() as SOOrder;
                            soDoc.OrderType = "CM";
                            soDoc.CustomerOrderNbr = amazonData?.Api_orderid;
                            soDoc.OrderDate = amazonData?.Api_date;
                            soDoc.RequestDate = amazonData?.Api_date;
                            soDoc.CustomerID = SelectFrom<BAccount>.Where<BAccount.acctCD.IsEqual<P.AsString>>.View.Select(baseGraph, "SPFJP").TopFirst?.BAccountID;
                            soDoc.OrderDesc = $"Amazon ({amazonData?.Api_trantype}) {amazonData?.Api_orderid}";
                            #endregion

                            #region User-Defined
                            // UserDefined - ORDERTYPE
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERTYPE", $"Amazon MCF");
                            // UserDefined - MKTPLACE
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "MKTPLACE", _marketplace);
                            // UserDefined - ORDERAMT
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERAMT", Math.Abs(amazonData?.Api_total ?? 0));
                            // UserDefined - PAYCHECK
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "PAYCHECK", paycheck);
                            #endregion

                            // Insert SOOrder
                            soGraph.Document.Insert(soDoc);

                            #region Set Currency
                            info = CurrencyInfoAttribute.SetDefaults<SOOrder.curyInfoID>(soGraph.Document.Cache, soGraph.Document.Current);
                            if (info != null)
                                soGraph.Document.Cache.SetValueExt<SOOrder.curyID>(soGraph.Document.Current, info.CuryID);
                            #endregion

                            #region Address/Contact
                            FAInfomation = SelectFrom<SOOrder>
                                               .InnerJoin<SOShippingContact>.On<SOOrder.shipContactID.IsEqual<SOShippingContact.contactID>>
                                               .InnerJoin<SOShippingAddress>.On<SOOrder.shipAddressID.IsEqual<SOShippingAddress.addressID>>
                                               .Where<SOOrder.orderType.IsEqual<P.AsString>
                                                 .And<SOOrder.customerOrderNbr.IsEqual<P.AsString>>>
                                               .View.SelectSingleBound(soGraph, null, "FA", amazonData?.Api_orderid);


                            if (FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault() != null)
                                SOShippingAddressAttribute.Copy(soGraph.Shipping_Address.Current, FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault());
                            if (FAInfomation.RowCast<SOShippingContact>().FirstOrDefault() != null)
                                SOShippingContactAttribute.CopyContact(soGraph.Shipping_Contact.Current, FAInfomation.RowCast<SOShippingContact>().FirstOrDefault());
                            #endregion

                            #region SOLine
                            // COD-REVENUE-JP
                            soGraph.Transactions.Insert(CreateSOLineObject(soGraph, "COD-REVENUE-JP", "COD_ITEMCHARGE", amazonData?.Api_coditemcharge));
                            // COD Fee
                            var _codFee = (Math.Abs(amazonData.Api_coditemcharge.Value) >= 0 && Math.Abs(amazonData.Api_coditemcharge.Value) < 30433) ? 432 :
                                          (Math.Abs(amazonData.Api_coditemcharge.Value) >= 30433 && Math.Abs(amazonData.Api_coditemcharge.Value) < 100649) ? 648 : 1080;
                            soGraph.Transactions.Insert(CreateSOLineObject(soGraph, "EC-SHIPPING", "COD Fee", _codFee * -1));
                            #endregion

                            #region Update Tax
                            if (isTaxCalculate)
                                soGraph.Document.Cache.SetValue<SOOrder.disableAutomaticTaxCalculation>(soGraph.Document.Current, false);
                            soGraph.Document.UpdateCurrent();

                            // Setting SO Tax
                            soGraph.Taxes.Current = soGraph.Taxes.Current ?? soGraph.Taxes.Insert(soGraph.Taxes.Cache.CreateInstance() as SOTaxTran);
                            soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxID>(soGraph.Taxes.Current, isTaxCalculate ? $"{_marketplace}ECZ" : $"{_marketplace}EC");
                            #endregion

                            lock (thisLock)
                            {
                                #region Valid Payment check

                                ValidPaymentCheck(paycheck, GetCustomerDefaultPaymentMethodInfo(soDoc.CustomerID, baseGraph)?.CashAccountID, "Order", baseGraph);

                                #endregion

                                // Sales Order Save
                                ValidAndSaveOrder(soGraph, amazonData);
                            }

                            // Prepare Invoice
                            PrepareInvoiceAndOverrideTax(soGraph, soDoc);
                            #endregion
                        }
                        #endregion

                        #region Create Sales Order MCF COD (Spec 1.2.2.4)
                        // Order ID starts with ‘S’ and [Amount Description] contain ‘CODItemCharge’ and CODItemCharge.Amount < 0
                        else if ((amazonData?.Api_orderid?.ToUpper().StartsWith("S") ?? false) && Math.Abs(amazonData?.Api_coditemcharge ?? 0) + Math.Abs(amazonData?.Api_codfee ?? 0) == 0)
                        {
                            #region Create SO Type: CM
                            soGraph = PXGraph.CreateInstance<SOOrderEntry>();

                            #region Header
                            soDoc = soGraph.Document.Cache.CreateInstance() as SOOrder;
                            soDoc.OrderType = "CM";
                            soDoc = soGraph.Document.Cache.Insert(soDoc) as SOOrder;
                            soDoc.CustomerOrderNbr = amazonData?.Api_orderid;
                            soDoc.OrderDate = amazonData?.Api_date;
                            soDoc.RequestDate = amazonData?.Api_date;
                            soDoc.CustomerID = AmazonPublicFunction.GetMarketplaceCustomer(_marketplace);
                            soDoc.OrderDesc = $"Amazon ({amazonData?.Api_trantype}) {amazonData?.Api_orderid}";
                            #endregion

                            #region User-Defined
                            // UserDefined - ORDERTYPE
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERTYPE", $"Amazon MCF");
                            // UserDefined - MKTPLACE
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "MKTPLACE", _marketplace);
                            // UserDefined - ORDERAMT
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERAMT", Math.Abs(amazonData?.Api_total ?? 0));
                            // UserDefined - PAYCHECK
                            soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "PAYCHECK", paycheck);
                            #endregion

                            // Insert SOOrder
                            soGraph.Document.Cache.Update(soDoc);
                            #region Set Currency
                            info = CurrencyInfoAttribute.SetDefaults<SOOrder.curyInfoID>(soGraph.Document.Cache, soGraph.Document.Current);
                            if (info != null)
                                soGraph.Document.Cache.SetValueExt<SOOrder.curyID>(soGraph.Document.Current, info.CuryID);
                            #endregion

                            #region SOLine
                            // EC-SHIPPING
                            soGraph.Transactions.Insert(CreateSOLineObject(soGraph, "EC-SHIPPING", "MCF Shipping fee", Math.Abs(amazonData?.Api_total ?? 0)));
                            #endregion

                            #region Update Tax
                            if (isTaxCalculate)
                                soGraph.Document.Cache.SetValue<SOOrder.disableAutomaticTaxCalculation>(soGraph.Document.Current, false);
                            soGraph.Document.UpdateCurrent();
                            #endregion

                            // Sales Order Save
                            soGraph.Document.UpdateCurrent();

                            lock (thisLock)
                            {
                                #region Valid Payment check

                                ValidPaymentCheck(paycheck, GetCustomerDefaultPaymentMethodInfo(soDoc.CustomerID, baseGraph)?.CashAccountID, "Order", baseGraph);

                                #endregion

                                // Sales Order Save
                                ValidAndSaveOrder(soGraph, amazonData);
                            }

                            #region Create PaymentRefund
                            if (amazonData?.Api_total != 0)
                            {
                                paymentExt = soGraph.GetExtension<CreatePaymentExt>();
                                paymentExt.SetDefaultValues(paymentExt.QuickPayment.Current, soGraph.Document.Current);
                                paymentExt.QuickPayment.Current.ExtRefNbr = amazonData?.Api_settlementid;
                                paymentEntry = paymentExt.CreatePayment(paymentExt.QuickPayment.Current, soGraph.Document.Current, soDoc.OrderType == "CM" ? ARPaymentType.Refund : ARPaymentType.Payment);
                                paymentEntry.Document.Cache.SetValueExt<ARPayment.adjDate>(paymentEntry.Document.Current, amazonData?.Api_date);
                                paymentEntry.Save.Press();
                                paymentEntry.releaseFromHold.Press();
                                lock (thisLock)
                                    paymentEntry.release.Press();
                            }
                            #endregion

                            // Prepare Invoice
                            PrepareInvoiceAndOverrideTax(soGraph, soDoc, false);
                            #endregion
                        }
                        #endregion
                        #endregion
                        break;
                    case "REFUND_RETROCHARGE":
                        #region Transaction Type: Refund_Retrocharge

                        soGraph = PXGraph.CreateInstance<SOOrderEntry>();

                        #region Header
                        soDoc = soGraph.Document.Cache.CreateInstance() as SOOrder;
                        soDoc.OrderType = "CM";
                        soDoc.CustomerOrderNbr = amazonData?.Api_orderid;
                        soDoc.OrderDate = amazonData?.Api_date;
                        soDoc.RequestDate = amazonData?.Api_date;
                        soDoc.CustomerID = AmazonPublicFunction.GetMarketplaceCustomer(_marketplace);
                        soDoc.OrderDesc = $"Amazon ({amazonData?.Api_trantype}) {amazonData?.Api_orderid}";
                        #endregion

                        #region User-Defined
                        // UserDefined - ORDERTYPE
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERTYPE", $"Amazon {amazonData?.Api_trantype}");
                        // UserDefined - MKTPLACE
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "MKTPLACE", _marketplace);
                        // UserDefined - ORDERAMT
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERAMT", Math.Abs(amazonData?.Api_total ?? 0));
                        // UserDefined - PAYCHECK
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "PAYCHECK", paycheck);
                        #endregion

                        // Insert SOOrder
                        soGraph.Document.Insert(soDoc);

                        #region Set Currency
                        info = CurrencyInfoAttribute.SetDefaults<SOOrder.curyInfoID>(soGraph.Document.Cache, soGraph.Document.Current);
                        if (info != null)
                            soGraph.Document.Cache.SetValueExt<SOOrder.curyID>(soGraph.Document.Current, info.CuryID);
                        #endregion

                        #region Address/Contact
                        FAInfomation = SelectFrom<SOOrder>
                                           .InnerJoin<SOShippingContact>.On<SOOrder.shipContactID.IsEqual<SOShippingContact.contactID>>
                                           .InnerJoin<SOShippingAddress>.On<SOOrder.shipAddressID.IsEqual<SOShippingAddress.addressID>>
                                           .Where<SOOrder.orderType.IsEqual<P.AsString>
                                             .And<SOOrder.customerOrderNbr.IsEqual<P.AsString>>>
                                           .View.SelectSingleBound(soGraph, null, "FA", amazonData?.Api_orderid);


                        if (FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault() != null)
                            SOShippingAddressAttribute.Copy(soGraph.Shipping_Address.Current, FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault());
                        if (FAInfomation.RowCast<SOShippingContact>().FirstOrDefault() != null)
                            SOShippingContactAttribute.CopyContact(soGraph.Shipping_Contact.Current, FAInfomation.RowCast<SOShippingContact>().FirstOrDefault());
                        #endregion

                        #region SOLine
                        // API_TRAN_TYPE
                        soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: amazonData?.Api_trantype.Replace("_", " "),
                                               desc: null,
                                               unitPrice: Math.Abs(amazonData?.Api_total ?? 0)));
                        #endregion

                        #region Update Tax
                        if (isTaxCalculate)
                            soGraph.Document.Cache.SetValue<SOOrder.disableAutomaticTaxCalculation>(soGraph.Document.Current, false);
                        soGraph.Document.UpdateCurrent();

                        // Setting SO Tax
                        soGraph.Taxes.Current = soGraph.Taxes.Current ?? soGraph.Taxes.Insert(soGraph.Taxes.Cache.CreateInstance() as SOTaxTran);
                        soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxID>(soGraph.Taxes.Current, isTaxCalculate ? $"{_marketplace}ECZ" : $"{_marketplace}EC");
                        #endregion

                        lock (thisLock)
                        {
                            #region Valid Payment check

                            ValidPaymentCheck(paycheck, GetCustomerDefaultPaymentMethodInfo(soDoc.CustomerID, baseGraph)?.CashAccountID, "Order", baseGraph);

                            #endregion

                            // Sales Order Save
                            ValidAndSaveOrder(soGraph, amazonData);
                        }

                        #region Create PaymentRefund
                        if (amazonData?.Api_total != 0)
                        {
                            paymentExt = soGraph.GetExtension<CreatePaymentExt>();
                            paymentExt.SetDefaultValues(paymentExt.QuickPayment.Current, soGraph.Document.Current);
                            paymentExt.QuickPayment.Current.ExtRefNbr = amazonData?.Api_settlementid;
                            paymentEntry = paymentExt.CreatePayment(paymentExt.QuickPayment.Current, soGraph.Document.Current, ARPaymentType.Refund);
                            paymentEntry.Document.Cache.SetValueExt<ARPayment.adjDate>(paymentEntry.Document.Current, amazonData?.Api_date);
                            paymentEntry.Save.Press();
                            paymentEntry.releaseFromHold.Press();
                            lock (thisLock)
                                paymentEntry.release.Press();
                        }
                        #endregion

                        // Prepare Invoice
                        PrepareInvoiceAndOverrideTax(soGraph, soDoc);
                        #endregion
                        break;
                    case "ORDER_RETROCHARGE":
                        #region Transaction Type: Order_Retrocharge

                        soGraph = PXGraph.CreateInstance<SOOrderEntry>();

                        #region Header
                        soDoc = soGraph.Document.Cache.CreateInstance() as SOOrder;
                        soDoc.OrderType = "IN";
                        soDoc.CustomerOrderNbr = amazonData?.Api_orderid;
                        soDoc.OrderDate = amazonData?.Api_date;
                        soDoc.RequestDate = amazonData?.Api_date;
                        soDoc.CustomerID = AmazonPublicFunction.GetMarketplaceCustomer(_marketplace);
                        soDoc.OrderDesc = $"Amazon ({amazonData?.Api_trantype}) {amazonData?.Api_orderid}";
                        #endregion

                        #region User-Defined
                        // UserDefined - ORDERTYPE
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERTYPE", $"Amazon {amazonData?.Api_trantype}");
                        // UserDefined - MKTPLACE
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "MKTPLACE", _marketplace);
                        // UserDefined - ORDERAMT
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERAMT", Math.Abs(amazonData?.Api_total ?? 0));
                        // UserDefined - PAYCHECK
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "PAYCHECK", paycheck);
                        #endregion

                        // Insert SOOrder
                        soGraph.Document.Insert(soDoc);

                        #region Set Currency
                        info = CurrencyInfoAttribute.SetDefaults<SOOrder.curyInfoID>(soGraph.Document.Cache, soGraph.Document.Current);
                        if (info != null)
                            soGraph.Document.Cache.SetValueExt<SOOrder.curyID>(soGraph.Document.Current, info.CuryID);
                        #endregion

                        #region Address/Contact
                        FAInfomation = SelectFrom<SOOrder>
                                           .InnerJoin<SOShippingContact>.On<SOOrder.shipContactID.IsEqual<SOShippingContact.contactID>>
                                           .InnerJoin<SOShippingAddress>.On<SOOrder.shipAddressID.IsEqual<SOShippingAddress.addressID>>
                                           .Where<SOOrder.orderType.IsEqual<P.AsString>
                                             .And<SOOrder.customerOrderNbr.IsEqual<P.AsString>>>
                                           .View.SelectSingleBound(soGraph, null, "FA", amazonData?.Api_orderid);


                        if (FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault() != null)
                            SOShippingAddressAttribute.Copy(soGraph.Shipping_Address.Current, FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault());
                        if (FAInfomation.RowCast<SOShippingContact>().FirstOrDefault() != null)
                            SOShippingContactAttribute.CopyContact(soGraph.Shipping_Contact.Current, FAInfomation.RowCast<SOShippingContact>().FirstOrDefault());
                        #endregion

                        #region SOLine
                        // API_TRAN_TYPE
                        soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: amazonData?.Api_trantype.Replace("_", " "),
                                               desc: null,
                                               unitPrice: Math.Abs(amazonData?.Api_total ?? 0)));
                        #endregion

                        #region Update Tax
                        if (isTaxCalculate)
                            soGraph.Document.Cache.SetValue<SOOrder.disableAutomaticTaxCalculation>(soGraph.Document.Current, false);
                        soGraph.Document.UpdateCurrent();

                        // Setting SO Tax
                        soGraph.Taxes.Current = soGraph.Taxes.Current ?? soGraph.Taxes.Insert(soGraph.Taxes.Cache.CreateInstance() as SOTaxTran);
                        soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxID>(soGraph.Taxes.Current, isTaxCalculate ? $"{_marketplace}ECZ" : $"{_marketplace}EC");
                        #endregion

                        lock (thisLock)
                        {
                            #region Valid Payment check

                            ValidPaymentCheck(paycheck, GetCustomerDefaultPaymentMethodInfo(soDoc.CustomerID, baseGraph)?.CashAccountID, "Order", baseGraph);

                            #endregion

                            // Sales Order Save
                            ValidAndSaveOrder(soGraph, amazonData);
                        }

                        #region Create Payment
                        if (amazonData?.Api_total != 0)
                        {
                            paymentExt = soGraph.GetExtension<CreatePaymentExt>();
                            paymentExt.SetDefaultValues(paymentExt.QuickPayment.Current, soGraph.Document.Current);
                            paymentExt.QuickPayment.Current.ExtRefNbr = amazonData?.Api_settlementid;
                            paymentEntry = paymentExt.CreatePayment(paymentExt.QuickPayment.Current, soGraph.Document.Current, ARPaymentType.Payment);
                            paymentEntry.Document.Cache.SetValueExt<ARPayment.adjDate>(paymentEntry.Document.Current, amazonData?.Api_date);
                            paymentEntry.Save.Press();
                            paymentEntry.releaseFromHold.Press();
                            paymentEntry.release.Press();
                        }
                        #endregion

                        // Prepare Invoice
                        PrepareInvoiceAndOverrideTax(soGraph, soDoc);
                        #endregion
                        break;
                    case "COUPONREDEMPTIONFEE":
                    case "CHARGE BACK REFUND":
                    case "CHARGEBACK REFUND":
                    case "FBA INVENTORY FEE":
                    case "ADJUSTMENT":
                    case "DEBT":
                    case "LIGHTNING DEAL FEE":
                    case "SERVICE FEE":
                    case "OTHER":
                    case null:
                        #region Transaction Type: OTHER-TRANSACTION

                        soGraph = PXGraph.CreateInstance<SOOrderEntry>();

                        #region Header
                        soDoc = soGraph.Document.Cache.CreateInstance() as SOOrder;
                        soDoc.OrderType = amazonData?.Api_total > 0 ? "IN" : "CM";
                        soDoc.CustomerOrderNbr = amazonData?.Api_orderid; ;
                        soDoc.OrderDate = amazonData?.Api_date;
                        soDoc.RequestDate = amazonData?.Api_date;
                        soDoc.CustomerID = AmazonPublicFunction.GetMarketplaceCustomer(_marketplace);
                        soDoc.OrderDesc = $"Amazon ({amazonData?.Api_trantype}) {amazonData?.Api_orderid}";
                        #endregion

                        #region User-Defined
                        // UserDefined - ORDERTYPE
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERTYPE", $"Amazon {amazonData?.Api_trantype}");
                        // UserDefined - MKTPLACE
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "MKTPLACE", _marketplace);
                        // UserDefined - ORDERAMT (CM: Sum Amount * -1)
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERAMT", Math.Abs(amazonData?.Api_total ?? 0));
                        // UserDefined - PAYCHECK
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "PAYCHECK", paycheck);
                        #endregion

                        // Insert SOOrder
                        soGraph.Document.Insert(soDoc);

                        #region Set Currency
                        info = CurrencyInfoAttribute.SetDefaults<SOOrder.curyInfoID>(soGraph.Document.Cache, soGraph.Document.Current);
                        if (info != null)
                            soGraph.Document.Cache.SetValueExt<SOOrder.curyID>(soGraph.Document.Current, info.CuryID);
                        #endregion

                        #region Address/Contact
                        FAInfomation = SelectFrom<SOOrder>
                                           .InnerJoin<SOShippingContact>.On<SOOrder.shipContactID.IsEqual<SOShippingContact.contactID>>
                                           .InnerJoin<SOShippingAddress>.On<SOOrder.shipAddressID.IsEqual<SOShippingAddress.addressID>>
                                           .Where<SOOrder.orderType.IsEqual<P.AsString>
                                             .And<SOOrder.customerOrderNbr.IsEqual<P.AsString>>>
                                           .View.SelectSingleBound(soGraph, null, "FA", amazonData?.Api_orderid);


                        if (FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault() != null)
                            SOShippingAddressAttribute.Copy(soGraph.Shipping_Address.Current, FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault());
                        if (FAInfomation.RowCast<SOShippingContact>().FirstOrDefault() != null)
                            SOShippingContactAttribute.CopyContact(soGraph.Shipping_Contact.Current, FAInfomation.RowCast<SOShippingContact>().FirstOrDefault());
                        #endregion

                        #region SOLine
                        // API_TRAN_TYPE
                        soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: amazonData?.Api_trantype?.Replace("_", " "),
                                               desc: null,
                                               unitPrice: Math.Abs(amazonData?.Api_total ?? 0)));
                        #endregion

                        #region Update Tax
                        if (isTaxCalculate)
                            soGraph.Document.Cache.SetValue<SOOrder.disableAutomaticTaxCalculation>(soGraph.Document.Current, false);
                        soGraph.Document.UpdateCurrent();

                        // Setting SO Tax
                        soGraph.Taxes.Current = soGraph.Taxes.Current ?? soGraph.Taxes.Insert(soGraph.Taxes.Cache.CreateInstance() as SOTaxTran);
                        soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxID>(soGraph.Taxes.Current, isTaxCalculate ? $"{_marketplace}ECZ" : $"{_marketplace}EC");
                        #endregion

                        lock (thisLock)
                        {
                            #region Valid Payment check

                            ValidPaymentCheck(paycheck, GetCustomerDefaultPaymentMethodInfo(soDoc.CustomerID, baseGraph)?.CashAccountID, "Order", baseGraph);

                            #endregion

                            // Sales Order Save
                            ValidAndSaveOrder(soGraph, amazonData);
                        }

                        #region Create PaymentRefund
                        if (amazonData?.Api_total != 0)
                        {
                            paymentExt = soGraph.GetExtension<CreatePaymentExt>();
                            paymentExt.SetDefaultValues(paymentExt.QuickPayment.Current, soGraph.Document.Current);
                            paymentExt.QuickPayment.Current.ExtRefNbr = amazonData?.Api_settlementid;
                            paymentEntry = paymentExt.CreatePayment(paymentExt.QuickPayment.Current, soGraph.Document.Current, soDoc.OrderType == "CM" ? ARPaymentType.Refund : ARPaymentType.Payment);
                            paymentEntry.Document.Cache.SetValueExt<ARPayment.adjDate>(paymentEntry.Document.Current, amazonData?.Api_date);
                            paymentEntry.Save.Press();
                            paymentEntry.releaseFromHold.Press();
                            lock (thisLock)
                                paymentEntry.release.Press();
                        }
                        #endregion

                        // Prepare Invoice
                        PrepareInvoiceAndOverrideTax(soGraph, soDoc);
                        #endregion
                        break;
                    case "TRANSFER":
                        break;
                    default:
                        #region Transaction Type: Undefined Transactions

                        soGraph = PXGraph.CreateInstance<SOOrderEntry>();

                        #region Header
                        soDoc = soGraph.Document.Cache.CreateInstance() as SOOrder;
                        soDoc.OrderType = amazonData?.Api_total > 0 ? "IN" : "CM";
                        soDoc.CustomerOrderNbr = amazonData?.Api_orderid; ;
                        soDoc.OrderDate = amazonData?.Api_date;
                        soDoc.RequestDate = amazonData?.Api_date;
                        soDoc.CustomerID = AmazonPublicFunction.GetMarketplaceCustomer(_marketplace);
                        soDoc.OrderDesc = $"Amazon ({amazonData?.Api_trantype}) {amazonData?.Api_orderid}";
                        #endregion

                        #region User-Defined
                        // UserDefined - ORDERTYPE
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERTYPE", $"Amazon Undefined Transactions");
                        // UserDefined - MKTPLACE
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "MKTPLACE", _marketplace);
                        // UserDefined - ORDERAMT (CM: Sum Amount * -1)
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "ORDERAMT", Math.Abs(amazonData?.Api_total ?? 0));
                        // UserDefined - PAYCHECK
                        soGraph.Document.Cache.SetValueExt(soDoc, PX.Objects.CS.Messages.Attribute + "PAYCHECK", paycheck);
                        #endregion

                        // Insert SOOrder
                        soGraph.Document.Insert(soDoc);

                        #region Set Currency
                        info = CurrencyInfoAttribute.SetDefaults<SOOrder.curyInfoID>(soGraph.Document.Cache, soGraph.Document.Current);
                        if (info != null)
                            soGraph.Document.Cache.SetValueExt<SOOrder.curyID>(soGraph.Document.Current, info.CuryID);
                        #endregion

                        #region Address/Contact
                        FAInfomation = SelectFrom<SOOrder>
                                           .InnerJoin<SOShippingContact>.On<SOOrder.shipContactID.IsEqual<SOShippingContact.contactID>>
                                           .InnerJoin<SOShippingAddress>.On<SOOrder.shipAddressID.IsEqual<SOShippingAddress.addressID>>
                                           .Where<SOOrder.orderType.IsEqual<P.AsString>
                                             .And<SOOrder.customerOrderNbr.IsEqual<P.AsString>>>
                                           .View.SelectSingleBound(soGraph, null, "FA", amazonData?.Api_orderid);


                        if (FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault() != null)
                            SOShippingAddressAttribute.Copy(soGraph.Shipping_Address.Current, FAInfomation.RowCast<SOShippingAddress>().FirstOrDefault());
                        if (FAInfomation.RowCast<SOShippingContact>().FirstOrDefault() != null)
                            SOShippingContactAttribute.CopyContact(soGraph.Shipping_Contact.Current, FAInfomation.RowCast<SOShippingContact>().FirstOrDefault());
                        #endregion

                        #region SOLine
                        // API_TRAN_TYPE
                        soGraph.Transactions.Insert(
                            CreateSOLineObject(soGraph,
                                               inventoryCD: "EC-OTHERTRANSACTION",
                                               desc: amazonData?.Api_trantype,
                                               unitPrice: amazonData?.Api_total * (soDoc.OrderType == "CM" ? -1 : 1)));
                        #endregion

                        #region Update Tax
                        if (isTaxCalculate)
                            soGraph.Document.Cache.SetValue<SOOrder.disableAutomaticTaxCalculation>(soGraph.Document.Current, false);
                        soGraph.Document.UpdateCurrent();

                        // Setting SO Tax
                        soGraph.Taxes.Current = soGraph.Taxes.Current ?? soGraph.Taxes.Insert(soGraph.Taxes.Cache.CreateInstance() as SOTaxTran);
                        soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxID>(soGraph.Taxes.Current, isTaxCalculate ? $"{_marketplace}ECZ" : $"{_marketplace}EC");
                        #endregion

                        lock (thisLock)
                        {
                            #region Valid Payment check

                            ValidPaymentCheck(paycheck, GetCustomerDefaultPaymentMethodInfo(soDoc.CustomerID, baseGraph)?.CashAccountID, "Order", baseGraph);

                            #endregion

                            // Sales Order Save
                            ValidAndSaveOrder(soGraph, amazonData);
                        }

                        #region Create PaymentRefund
                        if (amazonData?.Api_total != 0)
                        {
                            paymentExt = soGraph.GetExtension<CreatePaymentExt>();
                            paymentExt.SetDefaultValues(paymentExt.QuickPayment.Current, soGraph.Document.Current);
                            paymentExt.QuickPayment.Current.ExtRefNbr = amazonData?.Api_settlementid;
                            paymentEntry = paymentExt.CreatePayment(paymentExt.QuickPayment.Current, soGraph.Document.Current, soDoc.OrderType == "CM" ? ARPaymentType.Refund : ARPaymentType.Payment);
                            paymentEntry.Document.Cache.SetValueExt<ARPayment.adjDate>(paymentEntry.Document.Current, amazonData?.Api_date);
                            paymentEntry.Save.Press();
                            paymentEntry.releaseFromHold.Press();
                            lock (thisLock)
                                paymentEntry.release.Press();
                        }
                        #endregion

                        // Prepare Invoice
                        PrepareInvoiceAndOverrideTax(soGraph, soDoc);
                        #endregion
                        break;
                }
            }
        }

        /// <summary> Sales Order Prepare Invoice </summary>
        public static void PrepareInvoiceAndOverrideTax(SOOrderEntry soGraph, SOOrder soDoc, bool IsOverrideTax = true)
        {
            // Prepare Invoice
            try
            {
                soGraph.SelectTimeStamp();
                soGraph.Document.Current.tstamp = soGraph.TimeStamp;
                soGraph.releaseFromHold.Press();
                soGraph.prepareInvoice.Press();
            }
            // Prepare Invoice Success
            catch (PXRedirectRequiredException ex)
            {
                #region Override Invoice Infomation
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

        /// <summary> Create Payment CHARGES Object </summary>
        public static ARPaymentChargeTran CreatePaymentCHARGESObject(ARPaymentEntry arGraph, string entryTypeID, decimal? amount)
        {
            var chargeTrans = arGraph.PaymentCharges.Cache.CreateInstance() as ARPaymentChargeTran;
            chargeTrans.EntryTypeID = entryTypeID;
            chargeTrans.CuryTranAmt = amount;
            return chargeTrans;
        }

        /// <summary> Create SOLine Object </summary>
        public static SOLine CreateSOLineObject(SOOrderEntry soGraph, string inventoryCD, string desc, decimal? unitPrice, decimal? orderQty = 1, decimal? discountAmt = 0, string taxCategoryID = null, bool changeAccount = false)
        {
            var soTrans = soGraph.Transactions.Cache.CreateInstance() as SOLine;
            soTrans.InventoryID = AmazonPublicFunction.GetInvetoryitemID(soGraph, inventoryCD);
            soTrans.OrderQty = orderQty;
            if (!string.IsNullOrEmpty(desc))
                soTrans.TranDesc = desc;
            soTrans.CuryUnitPrice = unitPrice;
            if (!string.IsNullOrEmpty(taxCategoryID))
                soTrans.TaxCategoryID = taxCategoryID;
            if (discountAmt != 0)
                soTrans.CuryDiscAmt = discountAmt;
            if (changeAccount)
            {
                soTrans.SalesAcctID = PX.Objects.IN.InventoryItem.PK.Find(soGraph, soTrans.InventoryID)?.SalesAcctID;
                soTrans.SalesSubID = PX.Objects.IN.InventoryItem.PK.Find(soGraph, soTrans.InventoryID)?.SalesSubID;
            }
            return soTrans;
        }

        /// <summary> 檢查和儲存SalesOrder </summary>
        public static void ValidAndSaveOrder(SOOrderEntry soGraph, AmazonExcelEntity amazonData)
        {
            soGraph.Document.UpdateCurrent();
            if (amazonData?.Api_total == 0 && soGraph.Transactions.Cache.Inserted.Count() == 0)
                throw new Exception("Zero total will not be processed");
            soGraph.Save.Press();
        }

        /// <summary> 檢查 Payment Check unique </summary>
        public static void ValidPaymentCheck(string paycheck, int? CashAccountID, string type, PXGraph baseGraph)
        {
            object existsPayment = null;
            var cashAcctInfo = CashAccount.PK.Find(baseGraph, CashAccountID);

            if (type == "Order")
                existsPayment = SelectFrom<SOOrderKvExt>
                             .Where<SOOrderKvExt.fieldName.IsEqual<P.AsString>
                               .And<SOOrderKvExt.valueString.IsEqual<P.AsString>>>
                             .View.Select(baseGraph, PX.Objects.CS.Messages.Attribute + "PAYCHECK", paycheck).TopFirst;
            else
                existsPayment = SelectFrom<ARRegisterKvExt>
                                 .Where<ARRegisterKvExt.fieldName.IsEqual<P.AsString>
                                   .And<ARRegisterKvExt.valueString.IsEqual<P.AsString>>>
                                 .View.Select(baseGraph, PX.Objects.CS.Messages.Attribute + "PAYCHECK", paycheck).TopFirst;
            if ((cashAcctInfo.GetExtension<CashAccountExtension>()?.UsrCheckPaymentUnique ?? false) && existsPayment != null)
                throw new PXException($"Duplicate Payment: {paycheck} has been created");
        }

        /// <summary> 取得 Custoemr Default Payment method Information </summary>
        public static CustomerPaymentMethod GetCustomerDefaultPaymentMethodInfo(int? CustomerID, PXGraph baseGraph)
            => SelectFrom<CustomerPaymentMethod>
                               .InnerJoin<Customer>.On<CustomerPaymentMethod.bAccountID.IsEqual<Customer.bAccountID>
                                                  .And<CustomerPaymentMethod.paymentMethodID.IsEqual<Customer.defPaymentMethodID>>>
                               .Where<Customer.bAccountID.IsEqual<P.AsInt>>
                               .View.Select(baseGraph, CustomerID).TopFirst;

        public static string GetUNIXTimestamp(DateTime? _data)
        {
            if (!_data.HasValue)
                return string.Empty;
            // Get the offset from current time in UTC time
            DateTimeOffset dto = new DateTimeOffset(_data.Value);
            // Get the unix timestamp in seconds
            string unixTime = dto.ToUnixTimeSeconds().ToString();
            // Get the unix timestamp in seconds, and add the milliseconds
            string unixTimeMilliSeconds = dto.ToUnixTimeMilliseconds().ToString();
            return unixTimeMilliSeconds;
        }
    }

    public partial class AmazonPaymentUploadFileter : IBqlTable
    {
        [PXDecimal()]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXUIField(DisplayName = "API Total", Enabled = false)]
        public virtual decimal? ApiTotal { get; set; }
        public abstract class apiTotal : PX.Data.BQL.BqlDecimal.Field<apiTotal> { }
    }

    public class LUMApiDeleteFilter : IBqlTable
    {
        [PXDate]
        [PXUIField(DisplayName = "Delete From")]
        public virtual DateTime? DeleteFrom { get; set; }
        public abstract class deleteFrom : PX.Data.BQL.BqlDateTime.Field<deleteFrom> { }

        [PXDate]
        [PXUIField(DisplayName = "Delete To")]
        public virtual DateTime? DeleteTo { get; set; }
        public abstract class deleteTo : PX.Data.BQL.BqlDateTime.Field<deleteTo> { }
    }

    public class AmazonExcelEntity
    {
        public virtual string API_Marketplace { get; set; }

        public virtual DateTime? Api_date_1 { get; set; }

        public virtual DateTime? Api_date { get; set; }

        public virtual string Api_settlementid { get; set; }

        public virtual string Api_trantype { get; set; }

        public virtual string Api_orderid { get; set; }

        public virtual string Api_sku { get; set; }

        public virtual string Api_description { get; set; }

        public virtual Decimal? Api_productsales { get; set; }

        public virtual Decimal? Api_producttax { get; set; }

        public virtual Decimal? Api_shipping { get; set; }

        public virtual Decimal? Api_shippingtax { get; set; }

        public virtual Decimal? Api_giftwrap { get; set; }

        public virtual Decimal? Api_giftwraptax { get; set; }

        public virtual Decimal? Api_regulatoryfee { get; set; }

        public virtual Decimal? Api_taxonregulatoryfee { get; set; }

        public virtual Decimal? Api_promotion { get; set; }

        public virtual Decimal? Api_promotiontax { get; set; }

        public virtual Decimal? Api_whtax { get; set; }

        public virtual Decimal? Api_sellingfee { get; set; }

        public virtual Decimal? Api_fbafee { get; set; }

        public virtual Decimal? Api_othertranfee { get; set; }

        public virtual Decimal? Api_otherfee { get; set; }

        public virtual Decimal? Api_total { get; set; }

        public virtual Decimal? Api_cod { get; set; }

        public virtual Decimal? Api_codfee { get; set; }

        public virtual Decimal? Api_coditemcharge { get; set; }

        public virtual Decimal? Api_points { get; set; }
    }
}

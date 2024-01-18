using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Note = PX.Data.Note;

namespace PX.Objects.SO
{
    public class SOShipmentEntry_Extension : PXGraphExtension<SOShipmentEntry>
    {
        #region Constant Strings / Strng array
        public const string Attr_QTYCARTON  = "QTYCARTON";
        public const string Attr_NETWHTCART = "NETWHTCART";
        public const string Attr_GRSWHTCART = "GRSWHTCART";
        public const string Attr_PLTAIRUS   = "PLTAIRUS";
        public const string Attr_PLTSEAUS   = "PLTSEAUS";
        public const string Attr_PLTAIREU   = "PLTAIREU";
        public const string Attr_PLTSEAEU   = "PLTSEAEU";
        public const string ShipVia_Air     = "AIR";
        public const string ShipVia_Ocean   = "OCEAN";
        public const string Attr_PALLETSIZE = "PALLETSIZE";
        public const string Attr_PLTWHT     = "PLTWHT";
        public const string Attr_TOTALGW    = "TOTALGW";
        public const string Attr_CBM        = "CBM";

        public string[] InclCountries_EU = new string[] { "BE", "DE", "ES", "FR", "IT", "NL", "SE" };
        #endregion

        #region Override Methods
        public override void Initialize()
        {
            base.Initialize();

            Base.report.AddMenuAction(PackingList);
            Base.report.AddMenuAction(CommercialInvoice);
            Base.report.AddMenuAction(packingListOld);
        }
        #endregion

        #region Attribute Classes
        //Attribute : Qty Per Carton, ID = QTYCARTON
        public class QtyPerCartonAttr : BqlString.Constant<QtyPerCartonAttr>
        {
            public QtyPerCartonAttr() : base(Attr_QTYCARTON) { }
        }

        //Attribute : Gross Weight Per Carton, ID = GRSWHTCART
        public class GrossWeightPerCartonAttr : BqlString.Constant<GrossWeightPerCartonAttr>
        {
            public GrossWeightPerCartonAttr() : base(Attr_GRSWHTCART) { }
        }

        //Attribute : Net Weight Per Carton, ID = NETWHTCART
        public class NetWeightPerCartonAttr : BqlString.Constant<NetWeightPerCartonAttr>
        {
            public NetWeightPerCartonAttr() : base(Attr_NETWHTCART) { }
        }

        //Attribute : Cartons Per Pallet, ID = PLTAIREU
        public class CartonsPerPalletAttrAirEU : BqlString.Constant<CartonsPerPalletAttrAirEU>
        {
            public CartonsPerPalletAttrAirEU() : base(Attr_PLTAIREU) { }
        }

        //Attribute : Cartons Per Pallet, ID = PLTAIRUS
        public class CartonsPerPalletAttrAirUS : BqlString.Constant<CartonsPerPalletAttrAirUS>
        {
            public CartonsPerPalletAttrAirUS() : base(Attr_PLTAIRUS) { }
        }

        //Attribute : Cartons Per Pallet, ID = PLTSEAEU
        public class CartonsPerPalletAttrSeaEU : BqlString.Constant<CartonsPerPalletAttrSeaEU>
        {
            public CartonsPerPalletAttrSeaEU() : base(Attr_PLTSEAEU) { }
        }

        //Attribute : Cartons Per Pallet, ID = PLTSEAUS
        public class CartonsPerPalletAttrSeaUS : BqlString.Constant<CartonsPerPalletAttrSeaUS>
        {
            public CartonsPerPalletAttrSeaUS() : base(Attr_PLTSEAUS) { }
        }

        public class PalletSizeAttr : BqlString.Constant<PalletSizeAttr>
        {
            public PalletSizeAttr() : base(Attr_PALLETSIZE) { }
        }
        #endregion

        #region Event Handlers
        protected void _(Events.RowUpdated<SOShipment> e, PXRowUpdated baseHanlder)
        {
            baseHanlder?.Invoke(e.Cache, e.Args);

            var newValue = (string)GetShipmentUDF(e.Cache, e.Row, Attr_PLTWHT);
            var oldValue = (string)GetShipmentUDF(e.Cache, e.OldRow, Attr_PLTWHT);

            if (string.Compare(newValue, oldValue) != 0)
            {
                UpdateShipmentUDF(Attr_TOTALGW, CalcTotalShipWeight(e.Cache, e.Row, Base.Packages.View.SelectMulti().RowCast<SOPackageDetailEx>().ToList()));
            }
        }

        protected virtual void _(Events.FieldUpdated<SOPackageDetailExt.usrShipmentSplitLineNbr> e)
        {
            if (e.NewValue == null) { return; }

            var _shipLine = Base.Transactions.Cache.Cached.RowCast<SOShipLine>().Where(x => x.LineNbr == (int?)e.NewValue).SingleOrDefault();

            e.Cache.SetValue<SOPackageDetail.qty>(e.Row, _shipLine.ShippedQty);
            e.Cache.SetValueExt<SOPackageDetail.inventoryID>(e.Row, _shipLine.InventoryID);
            //Box
            var _defBoxID = SelectFrom<CSBox>.Where<CSBox.activeByDefault.IsEqual<True>>.View.Select(Base).TopFirst?.BoxID;
            if (_defBoxID != null) e.Cache.SetValueExt<SOPackageDetail.boxID>(e.Row, _defBoxID);
        }

        protected virtual void _(Events.FieldUpdated<SOPackageDetailEx.inventoryID> e)
        {
            Guid? itemNoteID = InventoryItem.PK.Find(Base, (int)e.NewValue)?.NoteID;

            e.Cache.SetValueExt<SOPackageDetailExt.usrNWCarton>(e.Row, Convert.ToDecimal(CSAnswers.PK.Find(Base, itemNoteID, Attr_NETWHTCART)?.Value ?? "0"));
            e.Cache.SetValueExt<SOPackageDetailExt.usrGWCarton>(e.Row, Convert.ToDecimal(CSAnswers.PK.Find(Base, itemNoteID, Attr_GRSWHTCART)?.Value ?? "0"));

            //TotalCartons
            decimal qtyPerCarton = Convert.ToDecimal(CSAnswers.PK.Find(Base, itemNoteID, Attr_QTYCARTON)?.Value ?? "0");

            decimal totalCartons = Math.Ceiling((e.Row as SOPackageDetailEx).Qty.Value / qtyPerCarton);

            e.Cache.SetValueExt<SOPackageDetailExt.usrTotalCartons>(e.Row, (int)totalCartons);
            //Carton No
            e.Cache.SetValueExt<SOPackageDetailExt.customRefNbr2>(e.Row, totalCartons);

            string attrPLTValue = null;

            if (InclCountries_EU.Any(a => a == Base.Shipping_Address.Current?.CountryID))
            {
                attrPLTValue = Base.Document.Current?.ShipVia == ShipVia_Air ? Attr_PLTAIREU : Attr_PLTSEAEU;
            }
            else
            {
                attrPLTValue = Base.Document.Current?.ShipVia == ShipVia_Air ? Attr_PLTAIRUS : Attr_PLTSEAUS;
            }

            totalCartons = Convert.ToDecimal(CSAnswers.PK.Find(Base, itemNoteID, attrPLTValue)?.Value);

            e.Cache.SetValueExt<SOPackageDetailExt.usrTotalPallet>(e.Row, Math.Ceiling((e.Row as SOPackageDetailEx).GetExtension<SOPackageDetailExt>().UsrTotalCartons.GetValueOrDefault() / totalCartons));
            e.Cache.SetValueExt<SOPackageDetailExt.customRefNbr1>(e.Row, e.Cache.GetValue<SOPackageDetailExt.usrTotalPallet>(e.Row));
        }

        protected virtual void _(Events.FieldUpdated<SOPackageDetailExt.usrTotalCartons> e)
        {
            UpdateShipmentUDF(Attr_TOTALGW, CalcTotalShipWeight(Base.Document.Cache, Base.Document.Current, Base.Packages.View.SelectMulti().RowCast<SOPackageDetailEx>().ToList()));
        }

        protected virtual void _(Events.FieldUpdated<SOPackageDetailExt.usrGWCarton> e)
        {
            UpdateShipmentUDF(Attr_TOTALGW, CalcTotalShipWeight(Base.Document.Cache, Base.Document.Current, Base.Packages.View.SelectMulti().RowCast<SOPackageDetailEx>().ToList()));
        }

        protected virtual void _(Events.FieldUpdated<SOPackageDetailExt.usrTotalPallet> e)
        {
            UpdateShipmentUDF(Attr_TOTALGW, CalcTotalShipWeight(Base.Document.Cache, Base.Document.Current, Base.Packages.View.SelectMulti().RowCast<SOPackageDetailEx>().ToList()));
            UpdateShipmentUDF(Attr_CBM, CalcVolumnCBM(e.Cache));
        }

        protected virtual void _(Events.FieldUpdated<SOPackageDetailExt.usrPalletSize> e)
        {
            UpdateShipmentUDF(Attr_CBM, CalcVolumnCBM(e.Cache));
        }
        #endregion

        #region Actions
        public PXAction<SOShipment> PackingList;
        [PXButton(DisplayOnMainToolbar = false)]
        [PXUIField(DisplayName = "Print Packing List", Enabled = true, MapEnableRights = PXCacheRights.Select)]
        protected virtual IEnumerable packingList(PXAdapter adapter)
        {
            if (Base.Document.Current != null)
            {
                RedirectWithReport(new Dictionary<string, string>
                {
                    [nameof(SOShipment.ShipmentNbr)] = Base.Document.Current.ShipmentNbr
                }, "LM642005");
            }
            return adapter.Get();
        }

        public PXAction<SOShipment> CommercialInvoice;
        [PXButton(DisplayOnMainToolbar = false)]
        [PXUIField(DisplayName = "Print Commercial Invoice", Enabled = true, MapEnableRights = PXCacheRights.Select)]
        protected virtual IEnumerable commercialInvoice(PXAdapter adapter)
        {
            if (Base.Document.Current != null)
            {
                if (Base.Document.Current.Status == SOShipmentStatus.Open || Base.Document.Current.Status == SOShipmentStatus.Hold) Base.Actions.PressSave();

                var poLineData = SelectFrom<SOShipLine>
                                 .InnerJoin<SOLine>.On<SOShipLine.origOrderNbr.IsEqual<SOLine.orderNbr>
                                                   .And<SOShipLine.origOrderType.IsEqual<SOLine.orderType>>
                                                   .And<SOShipLine.origLineNbr.IsEqual<SOLine.lineNbr>>>
                                 .InnerJoin<SOLineSplit>.On<SOLine.orderNbr.IsEqual<SOLineSplit.orderNbr>
                                                        .And<SOLine.orderType.IsEqual<SOLineSplit.orderType>>
                                                        .And<SOLine.lineNbr.IsEqual<SOLineSplit.lineNbr>>>
                                 .InnerJoin<POLine>.On<SOLineSplit.pOType.IsEqual<POLine.orderType>
                                                   .And<SOLineSplit.pONbr.IsEqual<POLine.orderNbr>>
                                                   .And<SOLineSplit.pOLineNbr.IsEqual<POLine.lineNbr>>>
                                .Where<SOShipLine.shipmentNbr.IsEqual<P.AsString>
                                  .And<SOShipLine.shipmentType.IsEqual<P.AsString>>>
                                 .View.Select(Base, Base.Document.Current.ShipmentNbr, Base.Document.Current.ShipmentType);
                decimal sumUsrTotalCartons = 0;
                foreach (PXResult<SOShipLine> line in Base.Transactions.Select())
                {
                    if (Base.Document.Current.ShipmentType == "I")
                    {
                        var _soShipline = SOShipLine.PK.Find(Base, ((SOShipLine)line).ShipmentNbr, ((SOShipLine)line).LineNbr);
                        var _soLine = SOLine.PK.Find(Base, _soShipline.OrigOrderType, _soShipline.OrigOrderNbr, _soShipline.OrigLineNbr);
                        sumUsrTotalCartons += (decimal)(_soLine?.CuryUnitPrice ?? 0) * (decimal)((SOShipLine)line).ShippedQty;
                    }
                    else
                    {
                        var currentPOLine = poLineData.RowCast<POLine>().FirstOrDefault(x => x.LineNbr == ((SOShipLine)line).LineNbr);
                        sumUsrTotalCartons += (decimal)((currentPOLine?.CuryUnitCost ?? 0) * (decimal)((SOShipLine)line).ShippedQty);
                    }
                }

                var shipFreight = Base.Document.Cache.GetValueExt(Base.Document.Current, PX.Objects.CS.Messages.Attribute + "SPFREIGHT") as PXFieldState;
                var shipInsuname = Base.Document.Cache.GetValueExt(Base.Document.Current, PX.Objects.CS.Messages.Attribute + "SPINSUR") as PXFieldState;
                try
                {
                    sumUsrTotalCartons = sumUsrTotalCartons + decimal.Parse((string)shipFreight?.Value ?? "0") + decimal.Parse((string)shipInsuname?.Value ?? "0");
                }
                catch
                {
                    // 轉型失敗不做任何事
                }
                //var _reportID = "LM642010";
                //Dictionary<string, string> parameters = new Dictionary<string, string>();
                //parameters["ShipmentNbr"] = Base.Document.Current.ShipmentNbr;
                //parameters["TotalValueEn"] = Number2English(Convert.ToDecimal(sumUsrTotalCartons));
                //throw new PXReportRequiredException(parameters, _reportID, $"Report {_reportID}") { Mode = PXBaseRedirectException.WindowMode.New };

                RedirectWithReport(new Dictionary<string, string>
                {
                    ["ShipmentNbr"] = Base.Document.Current.ShipmentNbr,
                    ["TotalValueEn"] = Number2English(Convert.ToDecimal(sumUsrTotalCartons))
                }, "LM642010");
            }
            return adapter.Get();
        }

        public PXAction<SOShipment> packingListOld;
        [PXButton(DisplayOnMainToolbar = false)]
        [PXUIField(DisplayName = "Print Packing List (Old)", Enabled = true, MapEnableRights = PXCacheRights.Select)]
        protected virtual IEnumerable PackingListOld(PXAdapter adapter)
        {
            if (Base.Document.Current != null)
            {
                RedirectWithReport(new Dictionary<string, string>
                {
                    [nameof(SOShipment.ShipmentNbr)] = Base.Document.Current.ShipmentNbr
                }, "LM642004");
            }
            return adapter.Get();
        }
        #endregion

        #region Other Custom Methods
        /// <summary>
        /// Add a new field “Total Shipping Weight” in User-Defined Field, allow user to adjust the final gross weight based on the actual number.
        /// ROUND(Total(SOPackageDetailEx.UsrTotalCartons x SOPackageDetailEx.UsrGWCarton) + Attribute(PLTWHT) x Total(SOPackageDetailEx.UsrTotalPallet), 2)
        /// </summary>
        public virtual decimal? CalcTotalShipWeight(PXCache shipmentCache, SOShipment shipment, List<SOPackageDetailEx> packages)
        {
            decimal calcValue = 0m, totalPallet = 0m, palletWeight = Convert.ToDecimal(GetShipmentUDF(shipmentCache, shipment, Attr_PLTWHT) ?? "0");

            for (int i = 0; i < packages.Count; i++)
            {
                var packageExt = packages[i].GetExtension<SOPackageDetailExt>();

                calcValue   += (packageExt.UsrTotalCartons ?? 0m) * (packageExt.UsrGWCarton ?? 0m);
                totalPallet += packageExt.UsrTotalPallet ?? 0m;
            }

            return Math.Round(calcValue + (palletWeight * totalPallet), 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Add a new field “Volume (CBM)” in User-Defined Field, allow user to adjust the final CBM based on the actual number.
        /// ROUND(SUM(SOPackageDetailEx.UsrPalletSize.Note / 1,000,000 x SOPackageDetailEx.UsrTotalPallet), 2)
        /// </summary>
        public virtual decimal? CalcVolumnCBM(PXCache packageCache)
        {
            List<SOPackageDetailEx> packages = packageCache.Cached.RowCast<SOPackageDetailEx>().ToList();

            foreach (SOPackageDetailEx row in packageCache.Deleted)
            {
                packages.Remove(row);
            }

            decimal calcValue = 0m;

            for (int i = 0; i < packages.Count; i++)
            {
                var packageExt = packages[i].GetExtension<SOPackageDetailExt>();

                decimal volumn = Convert.ToDecimal(SelectFrom<Note>.Where<Note.noteID.IsEqual<@P.AsGuid>>.View
                                                                   .SelectSingleBound(packageCache.Graph, null, 
                                                                                      CSAttributeDetail.PK.Find(packageCache.Graph, Attr_PALLETSIZE, packageExt?.UsrPalletSize)?.NoteID)
                                                                   .TopFirst?.NoteText ?? "0");

                calcValue += Math.Round(volumn / 1000000m * (packageExt.UsrTotalPallet ?? 0m), 2, MidpointRounding.AwayFromZero);
            }

            return calcValue;
        }

        private object GetShipmentUDF(PXCache shipmentCache, SOShipment shipment, string attributeID)
        {
            var state = shipmentCache.GetValueExt(shipment, $"{CS.Messages.Attribute}{attributeID}") as PXFieldState;

            return state.Value;
        }

        private void UpdateShipmentUDF(string attributeID, object value)
        {
            Base.Document.Cache.SetValueExt(Base.Document.Current, $"{CS.Messages.Attribute}{attributeID}", value);
            Base.Document.Cache.MarkUpdated(Base.Document.Current);
        }

        protected virtual void RedirectWithReport(Dictionary<string, string> parameters, string reportID)
        {
            throw new PXReportRequiredException(parameters, reportID, $"Report {reportID}") { Mode = PXBaseRedirectException.WindowMode.New };
        }

        #region Number2English mothod
        public string Number2English(decimal num)
        {
            string nu = num.ToString("#00.00");
            string dollars = "";
            string cents = "";
            string tp = "";
            string[] temp;
            string[] tx = { "", "THOUSAND", "MILLION", "BILLION", "TRILLION" };

            if (decimal.Parse(nu) == 0) return "ZERO";
            else if (decimal.Parse(nu) <= 0) return "ERROR!! ";
            else
            { //處理小數點(通常是兩位)
                temp = nu.Split('.');
                string strx = temp[1].ToString();

                string cent = GetEnglish(strx);
                if (!cent.Equals("")) cents = cent + "CENTS";
            }

            //處理整數部分
            //先將資料格式化，只取出整數
            decimal x = Math.Truncate(decimal.Parse(nu));
            //格式化整數部分
            temp = x.ToString("#,0").Split(',');
            //利用整數,號檢查千、萬、百萬....
            int j = temp.Length - 1;

            for (int i = 0; i < temp.Length; i++)
            {
                tp = tp + GetEnglish(temp[i]);
                if (tp != "")
                {
                    tp = tp + tx[j] + " ";
                }
                else
                {
                    tp = tp + " ";
                }

                j = j - 1;
            }
            if (x == 0 && cents != "") // 如果整數部位= 0 ，且有小數
            {
                dollars = "ZERO AND" + cents + " ONLY.";
            }
            else
            {
                if (cents == "")
                {
                    dollars = tp + "DOLLARS ONLY.";
                }
                else
                {
                    dollars = tp + "DOLLARS AND" + cents + " ONLY.";
                }
            }
            return "SAY TOTAL US DOLLARS " + dollars.Replace("DOLLARS", "").Replace("ONLY.", "ONLY").Replace("  ", " ");
        }

        private string GetEnglish(string nu)
        {
            string x = "";
            string str1;
            string str2;
            string[] tr = { "", "ONE", "TWO", "THREE", "FOUR", "FIVE", "SIX", "SEVEN", "EIGHT", "NINE", "TEN", "ELEVEN", "TWELVE", "THIRTEEN", "FOURTEEN", "FIFTEEN", "SIXTEEN", "SEVENTEEN", "EIGHTEEN", "NINETEEN" };
            string[] ty = { "", "", "TWENTY", "THIRTY", "FORTY", "FIFTY", "SIXTY", "SEVENTY", "EIGHTY", "NINETY" };

            //處理百位數
            str1 = tr[int.Parse(nu) / 100] + " HUNDRED"; //EX  當315除以100 用於int 會取出3 ..對應到字串陣列，就是 Three
            if (str1.Equals(" HUNDRED")) str1 = ""; //如果結果是空值，表示沒有百分位
            //處理十位數
            int temp = int.Parse(nu) % 100;   //  當315 除100 會剩餘 15 

            if (temp < 20)
            {
                str2 = tr[temp]; //取字串陣列 
            }
            else
            {
                str2 = ty[(temp / 10)].ToString();  //十位數  10/20/30的數量確認 

                if (str2.Equals(""))
                {
                    str2 = tr[(temp % 10)];
                }
                else
                {
                    str2 = str2 + "-" + tr[(temp % 10)];  //十位數組成
                }

            }
            if (str1 == "" && str2 == "")
            {
                x = "";
            }
            else
            {
                x = str1 + " " + str2 + " ";
            }

            return x;
        }
        #endregion

        #endregion
    }
}

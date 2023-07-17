using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.IN;

namespace PX.Objects.SO
{
    public class SOPackageDetailExt : PXCacheExtension<SOPackageDetail>
    {
        #region Qty
        [PXDBQuantity]
        [PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Qty", Enabled = true)]
        public virtual decimal? Qty { get; set; }
        public abstract class qty : BqlDecimal.Field<qty> { }
        #endregion

        #region CustomRefNbr1
        [PXDBString(30, IsUnicode = true)]
        [PXUIField(DisplayName = "Pallet No.")]
        public virtual string CustomRefNbr1 { get; set; }
        public abstract class customRefNbr1 : BqlString.Field<customRefNbr1> { }
        #endregion

        #region CustomRefNbr2
        [PXDBString(30, IsUnicode = true)]
        [PXUIField(DisplayName = "Carton No.")]
        public virtual string CustomRefNbr2 { get; set; }
        public abstract class customRefNbr2 : BqlString.Field<customRefNbr2> { }
        #endregion

        #region UsrShipmentSplitLineNbr
        [PXDBInt]
        [PXUIField(DisplayName = "Shipment Split Line Nbr")]
        [PXSelector(typeof(Search<SOShipLine.lineNbr,
                           Where<SOShipLine.shipmentNbr, Equal<Current<SOPackageDetail.shipmentNbr>>,
                           And<SOShipLine.packedQty, NotEqual<SOShipLine.shippedQty>>>>),
                    typeof(SOShipLine.origOrderType),
                    typeof(SOShipLine.origOrderNbr),
                    typeof(SOShipLine.inventoryID),
                    typeof(SOShipLine.shippedQty),
                    typeof(SOShipLine.packedQty),
                    typeof(SOShipLine.uOM))]
        public virtual int? UsrShipmentSplitLineNbr { get; set; }
        public abstract class usrShipmentSplitLineNbr : BqlInt.Field<usrShipmentSplitLineNbr> { }
        #endregion

        #region UsrTotalCartons
        [PXDBInt]
        [PXUIField(DisplayName = "Total Cartons")]
        public virtual int? UsrTotalCartons { get; set; }
        public abstract class usrTotalCartons : BqlInt.Field<usrTotalCartons> { }
        #endregion

        #region UsrNWCarton
        // Write default logic in extension graph event because attribute value is Text.
        [PXDBDecimal(2)]
        [PXUIField(DisplayName = "N.W. (KGS)/Carton")]
        public virtual decimal? UsrNWCarton { get; set; }
        public abstract class usrNWCarton : PX.Data.BQL.BqlDecimal.Field<usrNWCarton> { }
        #endregion

        #region UsrGWCarton
        // Write default logic in extension graph event because attribute value is Text.
        [PXDBDecimal(2)]
        [PXUIField(DisplayName = "G.W. (KGS)/Carton")]
        public virtual decimal? UsrGWCarton { get; set; }
        public abstract class usrGWCarton : PX.Data.BQL.BqlDecimal.Field<usrGWCarton> { }
        #endregion

        #region UsrTotalPallet
        // Write default logic in extension graph event because attribute value is Text.
        [PXDBDecimal(0)]
        [PXUIField(DisplayName = "Total Pallet")]
        public virtual decimal? UsrTotalPallet { get; set; }
        public abstract class usrTotalPallet : PX.Data.BQL.BqlDecimal.Field<usrTotalPallet> { }
        #endregion

        #region UsrPalletSize
        [PXDBString(10, IsUnicode = true)]
        [PXUIField(DisplayName = "Pallet Size (CM)")]
        [PXSelector(typeof(SelectFrom<CSAttributeDetail>.Where<CSAttributeDetail.attributeID.IsEqual<SOShipmentEntry_Extension.PalletSizeAttr>>.SearchFor<CSAttributeDetail.valueID>),
                    typeof(CSAttributeDetail.valueID),
                    DescriptionField = typeof(CSAttributeDetail.description))]
        [PXDefault(typeof(SelectFrom<CSAttributeDetail>.Where<CSAttributeDetail.attributeID.IsEqual<SOShipmentEntry_Extension.PalletSizeAttr>>
                                                       .OrderBy<CSAttributeDetail.sortOrder.Asc>
                                                       .SearchFor<CSAttributeDetail.valueID>),
                   PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual string UsrPalletSize { get; set; }
        public abstract class usrPalletSize : PX.Data.BQL.BqlString.Field<usrPalletSize> { }
        #endregion
    }
}
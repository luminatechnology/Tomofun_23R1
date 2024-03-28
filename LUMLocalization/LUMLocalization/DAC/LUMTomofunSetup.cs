using System;
using PX.Data;
using LUMLocalization.Graph;

namespace LUMLocalization.DAC
{
    [Serializable]
    [PXCacheName("LUMTomofunSetup")]
    [PXPrimaryGraph(typeof(LUMTomofunSetupMaint))]
    public class LUMTomofunSetup : IBqlTable
    {
        #region DefPalletLength
        [PXDBDecimal(2)]
        [PXUIField(DisplayName = "Default Pallet Length")]
        public virtual decimal? DefPalletLength { get; set; }
        public abstract class defPalletLength : PX.Data.BQL.BqlDecimal.Field<defPalletLength> { }
        #endregion

        #region DefPalletWidth
        [PXDBDecimal(2)]
        [PXUIField(DisplayName = "Default Pallet Width")]
        public virtual decimal? DefPalletWidth { get; set; }
        public abstract class defPalletWidth : PX.Data.BQL.BqlDecimal.Field<defPalletWidth> { }
        #endregion

        #region DefPalletHeight
        [PXDBDecimal(2)]
        [PXUIField(DisplayName = "Default Pallet Height")]
        public virtual decimal? DefPalletHeight { get; set; }
        public abstract class defPalletHeight : PX.Data.BQL.BqlDecimal.Field<defPalletHeight> { }
        #endregion

        #region CreatedByID
        [PXDBCreatedByID()]
        public virtual Guid? CreatedByID { get; set; }
        public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
        #endregion

        #region CreatedByScreenID
        [PXDBCreatedByScreenID()]
        public virtual string CreatedByScreenID { get; set; }
        public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
        #endregion

        #region CreatedDateTime
        [PXDBCreatedDateTime()]
        public virtual DateTime? CreatedDateTime { get; set; }
        public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
        #endregion

        #region LastModifiedByID
        [PXDBLastModifiedByID()]
        public virtual Guid? LastModifiedByID { get; set; }
        public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
        #endregion

        #region LastModifiedByScreenID
        [PXDBLastModifiedByScreenID()]
        public virtual string LastModifiedByScreenID { get; set; }
        public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
        #endregion

        #region LastModifiedDateTime
        [PXDBLastModifiedDateTime()]
        public virtual DateTime? LastModifiedDateTime { get; set; }
        public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
        #endregion

        #region RequireReferenceShipmentID
        [PXDBBool]
        [PXUIField(DisplayName = "Require Amazon & Tomofun Reference / Shipment ID")]
        public virtual bool? RequireReferenceShipmentID { get; set; }
        public abstract class requireReferenceShipmentID : PX.Data.BQL.BqlBool.Field<requireReferenceShipmentID> { }
        #endregion
    }
}
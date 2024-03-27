using PX.Data;
using LUMLocalization.DAC;

namespace LUMLocalization.Graph
{
    public class LUMTomofunSetupMaint : PXGraph<LUMTomofunSetupMaint>
    {
        public PXSave<LUMTomofunSetup> Save;
        public PXCancel<LUMTomofunSetup> Cancel;
        public PXSelect<LUMTomofunSetup> Setup;
    }
}
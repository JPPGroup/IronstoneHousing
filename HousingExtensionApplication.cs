using Autodesk.AutoCAD.Runtime;
using Jpp.Ironstone.Core;
using Jpp.Ironstone.Core.ServiceInterfaces;
using Jpp.Ironstone.Housing;
using Unity;

[assembly: ExtensionApplication(typeof(HousingExtensionApplication))]
namespace Jpp.Ironstone.Housing
{
    public class HousingExtensionApplication : IIronstoneExtensionApplication
    {
        public ILogger Logger { get; set; }
        public static HousingExtensionApplication Current { get; private set; }

        public void CreateUI() { }

        public void Initialize()
        {
            Current = this;
            CoreExtensionApplication._current.RegisterExtension(this);
        }

        public void InjectContainer(IUnityContainer container)
        {
            Logger = container.Resolve<ILogger>();
        }

        public void Terminate() { }
    }
}

namespace BetterSort.Accuracy {
  using BetterSort.Accuracy.External;
  using BetterSort.Accuracy.Sorter;
  using BetterSort.Common.External;
  using HarmonyLib;
  using IPA;
  using SiraUtil.Attributes;
  using SiraUtil.Zenject;
  using IPALogger = IPA.Logging.Logger;

  [Plugin(RuntimeOptions.DynamicInit), Slog, NoEnableDisable]
  public class Plugin {
    public static bool IsTest { get; set; }

    [Init]
    public Plugin(IPALogger logger, Zenjector zenjector) {
      logger.Debug("Initialize()");

      zenjector.UseHttpService();
      zenjector.UseLogger(logger);
      zenjector.Install(Location.App, container => {
        container.Bind<Harmony>().FromInstance(new Harmony("BetterSort.Accuracy")).AsSingle();
        container.Bind<IPALogger>().FromInstance(logger).AsSingle();
        container.BindInterfacesAndSelfTo<Clock>().AsSingle();
        container.BindInterfacesAndSelfTo<AccuracyRepository>().AsSingle();
      });
      zenjector.Install<AccuracyInstaller>(Location.App);

      logger.Info("Initialized.");
    }
  }
}

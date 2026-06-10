using System;
using System.Collections.Generic;

namespace AbilityKit.Game.Flow
{
    internal sealed class ViewFeatureSubFeatureBuilder
    {
        private readonly ViewSubFeatureFactory _factory;

        public ViewFeatureSubFeatureBuilder(ViewSubFeatureFactory factory = null)
        {
            _factory = factory ?? new ViewSubFeatureFactory();
        }

        public void AddBattleViewSubFeatures(List<IViewSubFeature<BattleViewFeature>> subFeatures)
        {
            if (subFeatures == null) throw new ArgumentNullException(nameof(subFeatures));

            _factory.AddDefaultViewSubFeatures(subFeatures);
        }

        public void AddConfirmedViewSubFeatures(List<IViewSubFeature<ConfirmedBattleViewFeature>> subFeatures)
        {
            if (subFeatures == null) throw new ArgumentNullException(nameof(subFeatures));

            _factory.AddDefaultViewSubFeatures(subFeatures);
        }
    }

    internal sealed class ViewSubFeatureFactory
    {
        private readonly ViewSubFeatureModuleFactory _modules;

        public ViewSubFeatureFactory(ViewSubFeatureModuleFactory modules = null)
        {
            _modules = modules ?? new ViewSubFeatureModuleFactory();
        }

        public void AddDefaultViewSubFeatures<TFeature>(List<IViewSubFeature<TFeature>> subFeatures)
            where TFeature : class, IViewFeatureRuntime
        {
            var modules = _modules.CreateDefaultModules<TFeature>();
            for (var i = 0; i < modules.Count; i++)
            {
                modules[i].AddTo(subFeatures);
            }
        }
    }

    internal interface IViewSubFeatureModule<TFeature>
        where TFeature : class, IViewFeatureRuntime
    {
        string Name { get; }

        void AddTo(List<IViewSubFeature<TFeature>> subFeatures);
    }

    internal sealed class ViewSubFeatureModuleFactory
    {
        public IReadOnlyList<IViewSubFeatureModule<TFeature>> CreateDefaultModules<TFeature>()
            where TFeature : class, IViewFeatureRuntime
        {
            return new IViewSubFeatureModule<TFeature>[]
            {
                new ViewRuntimeSubFeatureModule<TFeature>(),
                new ViewPresentationSubFeatureModule<TFeature>(),
                new ViewEventSubFeatureModule<TFeature>(),
            };
        }
    }

    internal sealed class ViewRuntimeSubFeatureModule<TFeature> : IViewSubFeatureModule<TFeature>
        where TFeature : class, IViewFeatureRuntime
    {
        public string Name => "ViewRuntime";

        public void AddTo(List<IViewSubFeature<TFeature>> subFeatures)
        {
            subFeatures.Add(new ViewContextBindingSubFeature<TFeature>());
            subFeatures.Add(new ViewTimelineSubFeature<TFeature>());
            subFeatures.Add(new ViewVfxSubFeature<TFeature>());
            subFeatures.Add(new ViewBindingSubFeature<TFeature>());
        }
    }

    internal sealed class ViewPresentationSubFeatureModule<TFeature> : IViewSubFeatureModule<TFeature>
        where TFeature : class, IViewFeatureRuntime
    {
        public string Name => "ViewPresentation";

        public void AddTo(List<IViewSubFeature<TFeature>> subFeatures)
        {
            subFeatures.Add(new ViewFloatingTextSubFeature<TFeature>());
            subFeatures.Add(new ViewAreaViewsSubFeature<TFeature>());
        }
    }

    internal sealed class ViewEventSubFeatureModule<TFeature> : IViewSubFeatureModule<TFeature>
        where TFeature : class, IViewFeatureRuntime
    {
        public string Name => "ViewEvents";

        public void AddTo(List<IViewSubFeature<TFeature>> subFeatures)
        {
            subFeatures.Add(new ViewEventSinkSubFeature<TFeature>());
            subFeatures.Add(new ViewEventAdaptersSubFeature<TFeature>());
        }
    }
}

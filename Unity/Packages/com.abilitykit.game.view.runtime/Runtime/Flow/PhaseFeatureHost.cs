using System;
using System.Collections.Generic;
using AbilityKit.Game.View.Foundation;

namespace AbilityKit.Game.View.Flow
{
    public sealed class PhaseFeatureHost<TContext, TFeature> where TFeature : class, IPhaseFeature<TContext>
    {
        public delegate void FeatureContextAction(TFeature feature, in TContext ctx);
        public delegate void FeatureTickAction(TFeature feature, in TContext ctx, float deltaTime);

        private readonly List<TFeature> _features;
        private readonly Action<string>? _fail;
        private readonly FeatureContextAction _attachFeature;
        private readonly FeatureContextAction _detachFeature;
        private readonly FeatureTickAction _tickFeature;
        private bool _isAttached;

        public PhaseFeatureHost(
            Action<string>? fail = null,
            int initialCapacity = 8,
            FeatureContextAction? attachFeature = null,
            FeatureContextAction? detachFeature = null,
            FeatureTickAction? tickFeature = null)
        {
            _features = new List<TFeature>(Math.Max(0, initialCapacity));
            _fail = fail;
            _attachFeature = attachFeature ?? DefaultAttachFeature;
            _detachFeature = detachFeature ?? DefaultDetachFeature;
            _tickFeature = tickFeature ?? DefaultTickFeature;
        }

        public IReadOnlyList<TFeature> Features => _features;
        public bool IsAttached => _isAttached;

        public void Add(TFeature feature, in TContext ctx)
        {
            if (feature == null) throw new ArgumentNullException(nameof(feature));
            _features.Add(feature);
            if (_isAttached)
            {
                _attachFeature(feature, in ctx);
            }
        }

        public bool Remove(TFeature feature, in TContext ctx)
        {
            if (feature == null) return false;
            var index = _features.IndexOf(feature);
            if (index < 0) return false;

            if (_isAttached)
            {
                _detachFeature(feature, in ctx);
            }

            _features.RemoveAt(index);
            return true;
        }

        public void AttachAll(in TContext ctx)
        {
            if (_isAttached)
            {
                _fail?.Invoke("AttachAll called while already attached.");
                return;
            }

            _isAttached = true;
            for (var i = 0; i < _features.Count; i++)
            {
                if (_features[i] != null)
                {
                    _attachFeature(_features[i], in ctx);
                }
            }
        }

        public void DetachAll(in TContext ctx)
        {
            if (!_isAttached)
            {
                _fail?.Invoke("DetachAll called while not attached.");
                return;
            }

            for (var i = _features.Count - 1; i >= 0; i--)
            {
                if (_features[i] != null)
                {
                    _detachFeature(_features[i], in ctx);
                }
            }

            _isAttached = false;
        }

        public void Clear(in TContext ctx)
        {
            if (_isAttached)
            {
                DetachAll(ctx);
            }

            _features.Clear();
        }

        public void Tick(in TContext ctx, float deltaTime)
        {
            for (var i = 0; i < _features.Count; i++)
            {
                if (_features[i] != null)
                {
                    _tickFeature(_features[i], in ctx, deltaTime);
                }
            }
        }

        public void OnGUI(in TContext ctx)
        {
            using var pooled = ViewFrameworkPools.GetList<IPhaseGuiFeature<TContext>>(_features.Count);
            var guiFeatures = pooled.List;

            for (var i = 0; i < _features.Count; i++)
            {
                if (_features[i] is IPhaseGuiFeature<TContext> gui)
                {
                    guiFeatures.Add(gui);
                }
            }

            for (var i = 0; i < guiFeatures.Count; i++)
            {
                guiFeatures[i].OnGUI(ctx);
            }
        }

        private static void DefaultAttachFeature(TFeature feature, in TContext ctx)
        {
            feature.OnAttach(ctx);
        }

        private static void DefaultDetachFeature(TFeature feature, in TContext ctx)
        {
            feature.OnDetach(ctx);
        }

        private static void DefaultTickFeature(TFeature feature, in TContext ctx, float deltaTime)
        {
            feature.Tick(ctx, deltaTime);
        }
    }
}

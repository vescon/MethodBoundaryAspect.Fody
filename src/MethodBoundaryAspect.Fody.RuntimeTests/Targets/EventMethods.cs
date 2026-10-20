using MethodBoundaryAspect.Fody.Attributes;
using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class EventMethods
    {
        private EventHandler _explicitHandler;

        [NameRecordingAspect]
        public event EventHandler FieldLike;

        [NameRecordingAspect]
        public event EventHandler Explicit
        {
            add => _explicitHandler += value;
            remove => _explicitHandler -= value;
        }

        public event EventHandler WithoutAspect;

        public void Raise()
        {
            FieldLike?.Invoke(this, EventArgs.Empty);
            _explicitHandler?.Invoke(this, EventArgs.Empty);
            WithoutAspect?.Invoke(this, EventArgs.Empty);
        }

        public void SubscribeWithoutAspect(EventHandler handler) => WithoutAspect += handler;
    }

    [NameRecordingAspect]
    public class DisableWeavingOnEventMethods
    {
        [DisableWeaving]
        public event EventHandler Disabled;

        public event EventHandler Enabled;

        public void Raise()
        {
            Disabled?.Invoke(this, EventArgs.Empty);
            Enabled?.Invoke(this, EventArgs.Empty);
        }
    }
}

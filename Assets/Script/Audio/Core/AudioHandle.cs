namespace Smoking.Audio
{
    /// <summary>A playback lease. Default, completed and recycled handles are safe no-ops.</summary>
    public readonly struct AudioHandle
    {
        private readonly IAudioHandleOwner owner;
        private readonly int slot;
        private readonly ulong version;

        internal AudioHandle(IAudioHandleOwner owner, int slot, ulong version)
        {
            this.owner = owner;
            this.slot = slot;
            this.version = version;
        }

        public bool IsValid => owner != null && owner.IsValid(slot, version);
        public void Stop(float fadeSeconds = 0f) => owner?.Stop(slot, version, fadeSeconds);
        public void Pause() => owner?.SetPaused(slot, version, true);
        public void Resume() => owner?.SetPaused(slot, version, false);
    }
}

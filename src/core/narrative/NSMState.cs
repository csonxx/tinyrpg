namespace Core.Narrative
{
    /// <summary>
    /// The NSM state enum represents the current operational mode of the Narrative State Machine.
    /// </summary>
    public enum NSMState
    {
        /// <summary>At title screen</summary>
        TITLE,

        /// <summary>Loading chapter data</summary>
        CHAPTER_LOADING,

        /// <summary>Scene running, no dialogue active</summary>
        SCENE_ACTIVE,

        /// <summary>Dialogue tree active</summary>
        DIALOGUE_ACTIVE,

        /// <summary>Cutscene playing</summary>
        CUTSCENE,

        /// <summary>Pause menu open</summary>
        MENU_OPEN,

        /// <summary>Chapter ended, showing completion</summary>
        CHAPTER_COMPLETE,

        /// <summary>Fatal error state</summary>
        ERROR
    }
}

using Core.Scene;

namespace Core.Narrative
{
    /// <summary>
    /// Interface for scene loading operations required by EpisodeStructure.
    ///
    /// This interface exists because Scene Management is a parallel Sprint 2 workstream.
    /// EpisodeStructure depends on this abstraction rather than a concrete implementation,
    /// allowing both systems to be developed independently and tested in isolation.
    ///
    /// A default stub implementation (DefaultEpisodeSceneManagement) is provided
    /// for development and testing before the full Scene Management system ships.
    /// </summary>
    public interface IEpisodeSceneManagement
    {
        /// <summary>
        /// Loads the specified scene with the given transition style.
        /// </summary>
        /// <param name="sceneId">The identifier of the scene to load.</param>
        /// <param name="transitionType">The visual transition style to use.</param>
        void LoadScene(string sceneId, TransitionType transitionType);
    }
}

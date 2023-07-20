public interface IBrainObserver
    {
        // Receive update from subject
        void ObserverUpdateSynapses(IBrainSubject subject);
        void ObserverUpdateSplines(IBrainSubject subject);
        void ObserverUpdateNeurons(IBrainSubject subject);
        void ObserverUpdateConvexHull(IBrainSubject subject);
        void ObserverUpdateSelection(IBrainSubject brainSubject);
        void ObserverUpdateTerrain(IBrainSubject brainSubject);
    }

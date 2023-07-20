using System.Threading.Tasks;
using System;
using System.Collections.Generic;

public interface ITerrainSubject {

        Task UpdateTerrain(PipQuery pipsQuery);

        // Attach an observer to the subject.
        void Attach(ITerrainObserver observer);

        // Detach an observer from the subject.
        void Detach(ITerrainObserver observer);

        void Notify();

        public HashSet<int> GetSelectedIDs();

}
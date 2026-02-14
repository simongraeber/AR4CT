using System;
using System.Collections.Generic;
using TriLibCore.General;

namespace TriLibCore.Mappers
{
    /// <summary>Represents a Viseme mapping candidate. A Lip-Sync Mapper may use this information to find suitable Blend-Shapes Keys.</summary>
    [Serializable]
    public class VisemeCandidate
    {
        /// <summary>
        /// Viseme type.
        /// </summary>
        public LipSyncViseme Viseme;

        /// <summary>
        /// List of candidate names.
        /// </summary>
        public List<string> CandidateNames;
    }
}
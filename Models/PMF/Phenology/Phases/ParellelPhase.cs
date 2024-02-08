﻿using System;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Phase is parallel to main phenology sequence, starting at an arbitary stage value and ending after the progression has accumulated to the 
    /// specified target
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class ParallelPhase : Model
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction target = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction progression = null;

        [Link(Type = LinkType.Scoped)] private Phenology phenology = null;

        // 2. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The phenological stage number at the start of this phase.</summary>
        [Description("Start")]
        public double Start { get; set; }

        /// <summary>Property specifying if we are currently with this phase</summary>
        public bool IsInPhase { get; set; }

        /// <summary>Fraction of phase that is complete (0-1).</summary>
        [JsonIgnore]
        public double FractionComplete
        {
            get
            {
                if (Target == 0.0)
                    return 1.0;
                else
                    return ProgressThroughPhase / Target;
            }
        }

        /// <summary>Accumulated units of progress through this phase.</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>Thermal time target to end this phase.</summary>
        [JsonIgnore]
        [Units("oD")]
        public double Target { get { return target.Value(); } }

        // 3. Public methods
        //-----------------------------------------------------------------------------------------------------------------
        
        /// <summary>Compute the phenological development following the main phenology loop</summary>
        [EventSubscribe("PostPhenology")]
        public void OnPostPhenology(object sender, EventArgs e)
        {
            if ((phenology.Stage >= Start) && (ProgressThroughPhase <= Target))
            {
                ProgressThroughPhase += progression.Value();
                IsInPhase = true;
            }
            else
            {
                IsInPhase = false;
            }
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()
        {
            ProgressThroughPhase = 0.0;
            IsInPhase = false;
        }

        // 4. Private method
        //-----------------------------------------------------------------------------------------------------------------

        [EventSubscribe("PhaseChanged")]
        private void onPhaseChanged(object sender, PhaseChangedType pct)
        {
            if (pct.StageNumber <= Start)
            {
                ResetPhase();   
            }
        }
        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void onSimulationCommencing(object sender, EventArgs e)
        {
            ResetPhase();
        }
    }
}




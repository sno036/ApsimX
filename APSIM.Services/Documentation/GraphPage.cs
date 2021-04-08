using System;
using System.Collections.Generic;

namespace APSIM.Services.Documentation
{
    /// <summary>
    /// A graph tag.
    /// </summary>
    public class GraphPage : Tag
    {
        /// <summary>
        /// The graphs to be displayed.
        /// </summary>
        public IEnumerable<Graph> Graphs { get; private set; }

        /// <summary>
        /// Constructs a graph tag instance.
        /// </summary>
        /// <param name="graphs">Graphs to be displayed.</param>
        /// <param name="indent">Indentation level.</param>
        public GraphPage(IEnumerable<Graph> graphs, int indent = 0) : base(indent) => Graphs = graphs;
    }
}

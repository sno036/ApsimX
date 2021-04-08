namespace APSIM.Services.Graphing
{
    public struct Marker
    {
        /// <summary>
        /// Marker type.
        /// </summary>
        public MarkerType Type { get; private set; }

        /// <summary>
        /// Marker size.
        /// </summary>
        /// <value></value>
        public MarkerSize Size { get; private set; }

        /// <summary>
        /// This is a modifier on marker size as a proportion of the original
        /// size. E.g. 0.5 for half size.
        /// </summary>
        public double SizeModifier { get; private set; }

        /// <summary>
        /// Creates a marker instance.
        /// </summary>
        /// <param name="type">Marker type.</param>
        /// <param name="size">Marker size.</param>
        /// <param name="modifier">Modifier on marker size, as a proportion of the original size.</param>
        public Marker(MarkerType type, MarkerSize size, double modifier)
        {
            Type = type;
            Size = size;
            SizeModifier = modifier;
        }
    }    
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF;
using Newtonsoft.Json;

namespace Models.Functions
{
    /// <summary>
    /// Uses the specified InterpolationMethod to determine sub daily values then calcualtes a value for the Response at each of these time steps
    /// and returns either the sum or average depending on the AgrevationMethod selected
    /// </summary>
    
    [Serializable]
    [Description("Uses the specified InterpolationMethod to determine sub daily values then calcualtes a value for the Response at each of these time steps and returns either the sum or average depending on the AgrevationMethod selected")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class HourlyInterpolation : Model, IFunction, ICustomDocumentation
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary> Method for interpolating Max and Min temperature to sub daily values </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IInterpolationMethod InterpolationMethod = null;

        /// <summary>The temperature response function applied to each sub daily temperature and averaged to give daily mean</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IIndexedFunction Response = null;

        /// <summary>Method used to agreagate sub daily values</summary>
        [Description("Method used to agregate sub daily temperature function")]
        public AgregationMethod agregationMethod { get; set; }

        /// <summary>Method used to agreagate sub daily values</summary>
        public enum AgregationMethod
        {
            /// <summary>Return average of sub daily values</summary>
            Average,
            /// <summary>Return sum of sub daily values</summary>
            Sum
        }


        /// <summary>Temperatures interpolated to sub daily values from Tmin and Tmax</summary>
        [JsonIgnore]
        public List<double> SubDailyTemperatures = null;

        /// <summary>Temperatures interpolated to sub daily values from Tmin and Tmax</summary>
        [JsonIgnore]
        public List<double> SubDailyResponse = null;

        /// <summary>Daily average temperature calculated from sub daily temperature interpolations</summary>
        public double Value(int arrayIndex = -1)
        {
            if (SubDailyResponse != null)
            {
                if (agregationMethod == AgregationMethod.Average)
                    return SubDailyResponse.Average();
                if (agregationMethod == AgregationMethod.Sum)
                    return SubDailyResponse.Sum();
                else
                    throw new Exception("invalid agregation method selected in " + this.Name + "temperature interpolation");
            }
            else
                return 0.0;
        }

        /// <summary> Set the sub dialy temperature values for the day then call temperature response function and set value for each sub daily period</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDailyInitialisation(object sender, EventArgs e)
        {
            SubDailyTemperatures = InterpolationMethod.SubDailyTemperatures();
            SubDailyResponse = new List<double>();
            foreach (double sdt in SubDailyTemperatures)
            {
                SubDailyResponse.Add(Response.ValueIndexed(sdt));
            }

        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);
            }
        }

    }

    /// <summary>
    /// A value is calculated from the mean of 3-hourly estimates of air temperature based on daily max and min temperatures.  
    /// </summary>
    [Serializable]
    [Description("A value is calculated from the mean of 3-hourly estimates of air temperature based on daily max and min temperatures\n\n" +
        "Eight interpolations of the air temperature are calculated using a three-hour correction factor." +
        "For each air three-hour air temperature, a value is calculated.  The eight three-hour estimates" +
        "are then averaged to obtain the daily value.")]
    [ValidParent(ParentType = typeof(HourlyInterpolation))]
    public class ThreeHourSin : Model, IInterpolationMethod
    {
        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>Factors used to multiply daily range to give diurnal pattern of temperatures between Tmax and Tmin</summary>
        public List<double> TempRangeFactors = null;
        
        /// <summary>
        /// Calculate temperatures at 3 hourly intervals from min and max using sin curve
        /// </summary>
        /// <returns>list of 8 temperature estimates for 3 hourly periods</returns>
        public List<double> SubDailyTemperatures()
        {
            List<double> sdts = new List<Double>();
            double diurnal_range = MetData.MaxT - MetData.MinT;
            foreach (double trf in TempRangeFactors)
            {
                sdts.Add(MetData.MinT + trf * diurnal_range);
            }
            return sdts;
        }

        /// <summary> Set the sub daily temperature range factor values at sowing</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            TempRangeFactors = t_range_fract();
        }

        /// <summary>Fraction_of of day's range_of for this 3 hr period</summary>
        public List<double> t_range_fract()
        {
            List<int> periods = Enumerable.Range(1, 8).ToList();
            List<double> trfs = new List<double>();
            // pre calculate t_range_fract for speed reasons
            foreach (int period in periods)
            {
                trfs.Add(0.92105
                        + 0.1140 * period
                        - 0.0703 * Math.Pow(period, 2)
                        + 0.0053 * Math.Pow(period, 3));
            }
            if (trfs.Count != 8)
                throw new Exception("Incorrect number of subdaily temperature estimations in " + this.Name + " temperature interpolation");
            return trfs;
        }
    }

    /// <summary>
    /// calculating the hourly temperature based on Tmax, Tmin and daylength
    /// At sunrise (th = 12 − d/2), the air temperature equals Tmin. The maximum temperature 
    /// is reached when th equals 12 + p h solar time.The default value for p is 1.5 h.
    /// The sinusoidal curve is followed until sunset.Then a transition takes place to an
    /// exponential decrease, proceeding to the minimum temperature of the next day.To
    /// plot this curve correctly, we first need the starting point, the temperature at sunset
    /// (Tsset).
    /// </summary>
    [Serializable]
    [Description("calculating the hourly temperature based on Tmax, Tmin and daylength")]
    [ValidParent(ParentType = typeof(HourlyInterpolation))]
    public class HourlySinPpAdjusted : Model, IInterpolationMethod
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        private const double P = 1.5;

        private const double TC = 4.0;

        /// <summary>
        /// Temperature at the most recent sunset
        /// </summary>
        [JsonIgnore]
        public double Tsset { get; set; }

        /// <summary> Set the sub daily temperature range factor values at sowing</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            
        }

        /// <summary>Creates a list of temperature range factors used to estimate daily temperature from Min and Max temp</summary>
        /// <returns></returns>
        public List<double> SubDailyTemperatures()
        {
            double d = MetData.CalculateDayLength(-6);
            double Tmin = MetData.MinT;
            double Tmax = MetData.MaxT;
            double TmaxB = MetData.YesterdaysMetData.MaxT;
            double TminA = MetData.TomorrowsMetData.MinT;
            double Hsrise = MetData.CalculateSunRise();
            double Hsset =  MetData.CalculateSunSet();
            
            List<double> sdts = new List<double>();
            
            for (int Th = 0; Th <= 23; Th++)
            {
                double Ta = 1.0;
                if (Th < Hsrise)
                {
                    //  Hour between midnight and sunrise
                    //  PERIOD A MaxTB is max. temperature, before day considered

                    //this is the sunset temperature of based on the previous day
                    double n = 24 - d;
                    Tsset = Tmin + (TmaxB - Tmin) *
                                    Math.Sin(Math.PI * (d / (d + 2 * P)));

                    Ta = (Tmin - Tsset * Math.Exp(-n / TC) +
                            (Tsset - Tmin) * Math.Exp(-(Th + 24 - Hsset) / TC)) /
                            (1 - Math.Exp(-n / TC));
                }
                else if (Th >= Hsrise & Th < 12 + P)
                {
                    // PERIOD B Hour between sunrise and normal time of MaxT
                    Ta = Tmin + (Tmax - Tmin) *
                            Math.Sin(Math.PI * (Th - Hsrise) / (d + 2 * P));
                }
                else if (Th >= 12 + P & Th < Hsset)
                {
                    // PERIOD C Hour between normal time of MaxT and sunset
                    //  MinTA is min. temperature, after day considered

                    Ta = TminA + (Tmax - TminA) *
                        Math.Sin(Math.PI * (Th - Hsrise) / (d + 2 * P));
                }
                else
                {
                    // PERIOD D Hour between sunset and midnight
                    Tsset = TminA + (Tmax - TminA) * Math.Sin(Math.PI * (d / (d + 2 * P)));
                    double n = 24 - d;
                    Ta = (TminA - Tsset * Math.Exp(-n / TC) +
                            (Tsset - TminA) * Math.Exp(-(Th - Hsset) / TC)) /
                            (1 - Math.Exp(-n / TC));
                }
                sdts.Add(Ta);
            }
            return sdts;
        }
    }

    /// <summary>An interface that defines what needs to be implemented by an organthat has a water demand.</summary>
    public interface IInterpolationMethod
    {
        /// <summary>Calculate temperature at specified periods during the day.</summary>
        List<double> SubDailyTemperatures();
    }

}
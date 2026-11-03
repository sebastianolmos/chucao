using Chucao.Views;

internal static class BirdFlightChecks
{
    public static void Run(Action<bool, string> check)
    {
        static bool Near(double a, double b) => Math.Abs(a - b) < 0.000001;
        static BirdFlightFrame At(double elapsed) => BirdFlightTimeline.Evaluate(60, elapsed, 5, 2, 2);

        var start = At(0);
        check(start.FromIndex == 0 && start.ToIndex == 0 && start.FacingLeft
            && start.State == BirdMotionState.Stopped && start.GrowthProgress == 0,
            "The bird waits at the first point facing left at its initial size");
        check(At(9.9) == start, "The first ten seconds are spent waiting without growing");
        var firstFlight = At(14);
        check(firstFlight.FromIndex == 0 && firstFlight.ToIndex == 1
            && firstFlight.State == BirdMotionState.Flying && firstFlight.FacingLeft
            && Near(firstFlight.FlightProgress, 0.5) && Near(firstFlight.GrowthProgress, 0.2),
            "The first flight lasts eight seconds and grows only during flight");
        var secondStop = At(18);
        check(secondStop.FromIndex == 1 && secondStop.ToIndex == 1 && !secondStop.FacingLeft
            && secondStop.State == BirdMotionState.Stopped && Near(secondStop.GrowthProgress, 0.4)
            && At(27.9) == secondStop,
            "Arrival flips the bird once and the next ten-second stop preserves size");
        check(At(31).FromIndex == 1 && Near(At(31).FlightProgress, 0.5)
            && At(46).FromIndex == 2 && Near(At(46).FlightProgress, 0.5)
            && At(59).FromIndex == 3 && Near(At(59).FlightProgress, 0.5),
            "The remaining flights last six, four and two seconds, separated by equal waits");
        var finish = At(60);
        check(finish.FromIndex == 4 && finish.ToIndex == 4 && finish.GrowthProgress == 1
            && finish.FacingLeft && finish.State == BirdMotionState.Stopped && At(80) == finish,
            "The final arrival, final size and final flip coincide with timer completion");

        var custom = BirdFlightTimeline.Evaluate(60, 13.5, 5, 3, 1);
        check(Near(custom.FlightProgress, 0.5) && Near(custom.GrowthProgress, 3.0 / 18),
            "Custom flight durations redistribute the remaining time among stops");
        var shortTimer = BirdFlightTimeline.Evaluate(10, 1.25, 5, 2, 2);
        check(shortTimer.State == BirdMotionState.Flying && shortTimer.FromIndex == 0
            && Near(shortTimer.FlightProgress, 0.5) && Near(shortTimer.GrowthProgress, 0.125)
            && BirdFlightTimeline.Evaluate(10, 10, 5, 2, 2).FromIndex == 4,
            "A short timer divides its duration into equal flights without waits and finishes on time");
        var shortArrival = BirdFlightTimeline.Evaluate(10, 2.5, 5, 2, 2);
        check(shortArrival.FromIndex == 1 && shortArrival.State == BirdMotionState.Flying
            && !shortArrival.FacingLeft && Near(shortArrival.FlightProgress, 0),
            "Equal flights flip instantly at arrival before continuing to the next point");
        var single = BirdFlightTimeline.Evaluate(60, 60, 1, 2, 2);
        check(single.FromIndex == 0 && single.GrowthProgress == 0 && single.FacingLeft,
            "A single-point route stays still without growth or flipping");
    }
}

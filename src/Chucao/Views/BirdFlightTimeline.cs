namespace Chucao.Views;

public enum BirdMotionState { Stopped, Flying }

public readonly record struct BirdFlightFrame(
    int FromIndex, int ToIndex, double FlightProgress, double GrowthProgress,
    bool FacingLeft, BirdMotionState State);

// Visual timing only: no independent clock, window, file access or countdown state.
public static class BirdFlightTimeline
{
    public static BirdFlightFrame Evaluate(double durationSeconds, double elapsedSeconds,
        int pointCount, double finalFlightSeconds, double earlierFlightIncrementSeconds)
    {
        if (!double.IsFinite(durationSeconds) || durationSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        if (!double.IsFinite(elapsedSeconds))
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        if (pointCount < 1)
            throw new ArgumentOutOfRangeException(nameof(pointCount));
        if (!double.IsFinite(finalFlightSeconds) || finalFlightSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(finalFlightSeconds));
        if (!double.IsFinite(earlierFlightIncrementSeconds) || earlierFlightIncrementSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(earlierFlightIncrementSeconds));

        int legs = pointCount - 1;
        if (legs == 0)
            return new(0, 0, 0, 0, true, BirdMotionState.Stopped);

        double requestedFlightTime = legs * finalFlightSeconds
            + earlierFlightIncrementSeconds * legs * (legs - 1) / 2;
        // When editing points/timings, consider whether all flights still fit the timer.
        // If they exceed it, give EVERY leg the same duration and omit all waits.
        // The direction flip occurs instantly at each arrival, without adding time.
        bool useEqualFlights = requestedFlightTime > durationSeconds;
        double totalFlightTime = useEqualFlights ? durationSeconds : requestedFlightTime;
        double waitPerPoint = Math.Max(0, durationSeconds - totalFlightTime) / legs;
        double elapsed = Math.Clamp(elapsedSeconds, 0, durationSeconds);
        if (elapsed >= durationSeconds)
            return new(legs, legs, 0, 1, legs % 2 == 0, BirdMotionState.Stopped);

        double cursor = 0;
        double completedFlightTime = 0;
        for (int from = 0; from < legs; from++)
        {
            if (elapsed < cursor + waitPerPoint)
                return new(from, from, 0, completedFlightTime / totalFlightTime,
                    from % 2 == 0, BirdMotionState.Stopped);
            cursor += waitPerPoint;
            double flightTime = useEqualFlights ? durationSeconds / legs
                : finalFlightSeconds + (legs - 1 - from) * earlierFlightIncrementSeconds;
            if (elapsed < cursor + flightTime)
            {
                double timeInFlight = elapsed - cursor;
                return new(from, from + 1, timeInFlight / flightTime,
                    (completedFlightTime + timeInFlight) / totalFlightTime,
                    from % 2 == 0, BirdMotionState.Flying);
            }
            cursor += flightTime;
            completedFlightTime += flightTime;
        }

        return new(legs, legs, 0, 1, legs % 2 == 0, BirdMotionState.Stopped);
    }
}

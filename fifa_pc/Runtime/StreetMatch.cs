namespace FifaStreetPc;

public readonly record struct MatchInput(
    float MoveX,
    float MoveY,
    bool Pass,
    bool Shoot,
    bool Trick,
    bool Panna = false,
    bool Gamebreaker = false,
    bool Juggle = false,
    bool Tackle = false);

public enum BallOwner
{
    None,
    Home,
    Away,
}

public enum CleanRoomMatchResult
{
    InProgress,
    Draw,
    HomeWin,
    AwayWin,
}

public enum CleanRoomMatchEndCondition
{
    Timed,
    FirstToGoals,
    GoalsOrPanna,
}

/// <summary>
/// Authored clean-room gameplay telemetry. These events describe the managed
/// simulation only; they are not decoded original-game events.
/// </summary>
public readonly record struct CleanRoomMatchEvent(
    int Sequence,
    int Frame,
    string Action,
    int HomeScore,
    int AwayScore,
    BallOwner Possession);

public sealed class StreetMatch
{
    private const float FieldWidth = 1.0f;
    private const float FieldHeight = 0.62f;
    private const float PossessionRadius = 0.18f;
    private const float ShotSpeed = 5.0f;
    private const float PassSpeed = 2.8f;
    private const float OpponentPressSpeed = 0.42f;
    private const double OpponentShotDelaySeconds = 0.75;
    private const double CleanRoomRestartDelaySeconds = 0.5;
    public const int MaximumGamebreakerMeter = 100;
    public const float MaximumBallHeight = 0.45f;
    private readonly bool opponentAiEnabled;
    private readonly bool cleanRoomRoundResetEnabled;
    private readonly CleanRoomMatchEndCondition endCondition;
    private readonly int targetGoals;
    private readonly int targetPannas;
    private bool shootWasDown;
    private bool trickWasDown;
    private bool pannaWasDown;
    private bool gamebreakerWasDown;
    private bool juggleWasDown;
    private bool tackleWasDown;
    private bool ballInFlight;
    private bool restartPending;
    private bool fullTimeRaised;
    private bool homeHasTouchedBall;
    private float ballVelocityX;
    private float ballVelocityY;
    private BallOwner flightTarget;
    private BallOwner flightShooter;
    private BallOwner restartOwner;
    private double restartSecondsRemaining;
    private double opponentPossessionSeconds;
    private double tackleProtectionSeconds;
    private readonly Dictionary<string, int> actionCounts = new(StringComparer.Ordinal);
    private readonly List<CleanRoomMatchEvent> eventHistory = new();

    public StreetMatch(
        bool enableOpponentAi = false,
        bool enableCleanRoomRoundReset = false,
        double matchDurationSeconds = 300.0,
        CleanRoomMatchEndCondition endCondition = CleanRoomMatchEndCondition.Timed,
        int targetGoals = 0,
        int targetPannas = 0)
    {
        if (double.IsNaN(matchDurationSeconds) ||
            double.IsInfinity(matchDurationSeconds) ||
            matchDurationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(matchDurationSeconds));
        }
        if (!Enum.IsDefined(endCondition))
        {
            throw new ArgumentOutOfRangeException(nameof(endCondition));
        }
        if ((endCondition == CleanRoomMatchEndCondition.FirstToGoals && targetGoals <= 0) ||
            (endCondition == CleanRoomMatchEndCondition.GoalsOrPanna &&
             (targetGoals <= 0 || targetPannas <= 0)))
        {
            throw new ArgumentOutOfRangeException(nameof(targetGoals));
        }
        if (endCondition == CleanRoomMatchEndCondition.FirstToGoals && targetPannas != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetPannas));
        }
        if (endCondition == CleanRoomMatchEndCondition.Timed &&
            (targetGoals != 0 || targetPannas != 0))
        {
            throw new ArgumentOutOfRangeException(nameof(targetGoals));
        }

        opponentAiEnabled = enableOpponentAi;
        cleanRoomRoundResetEnabled = enableCleanRoomRoundReset;
        this.endCondition = endCondition;
        this.targetGoals = targetGoals;
        this.targetPannas = targetPannas;
        HomePosition = (0.25f, 0.31f);
        AwayPosition = (0.75f, 0.31f);
        BallPosition = (0.50f, 0.31f);
        MatchDurationSeconds = matchDurationSeconds;
        RemainingSeconds = matchDurationSeconds;
        Possession = BallOwner.None;
        RoundNumber = 1;
        Result = CleanRoomMatchResult.InProgress;
        LastAction = "KICKOFF";
        actionCounts[LastAction] = 1;
        eventHistory.Add(new CleanRoomMatchEvent(
            Sequence: 0,
            Frame: 0,
            Action: LastAction,
            HomeScore: HomeScore,
            AwayScore: AwayScore,
            Possession: Possession));
    }

    public (float X, float Y) HomePosition { get; private set; }
    public (float X, float Y) AwayPosition { get; private set; }
    public (float X, float Y) BallPosition { get; private set; }
    public int HomeScore { get; private set; }
    public int AwayScore { get; private set; }
    public double RemainingSeconds { get; private set; }
    public double MatchDurationSeconds { get; }
    public int Frame { get; private set; }
    public int ActionSequence { get; private set; }
    public BallOwner Possession { get; private set; }
    public int TrickCount { get; private set; }
    public int PannaCount { get; private set; }
    public int GamebreakerCount { get; private set; }
    public int GamebreakerMeter { get; private set; }
    public int JuggleCount { get; private set; }
    public int JuggleChain { get; private set; }
    public int TackleCount { get; private set; }
    public float BallHeight { get; private set; }
    public int RoundNumber { get; private set; }
    public CleanRoomMatchResult Result { get; private set; }
    public string LastAction { get; private set; }
    public bool OpponentAiEnabled => opponentAiEnabled;
    public bool CleanRoomRoundResetEnabled => cleanRoomRoundResetEnabled;
    public CleanRoomMatchEndCondition EndCondition => endCondition;
    public int TargetGoals => targetGoals;
    public int TargetPannas => targetPannas;
    public bool RestartPending => restartPending;
    public IReadOnlyDictionary<string, int> ActionCounts => actionCounts;
    public IReadOnlyList<CleanRoomMatchEvent> EventHistory => eventHistory.AsReadOnly();
    public bool IsEnded => RemainingSeconds <= 0 || HasReachedTargetGoals;

    public void Tick(double deltaSeconds, MatchInput input)
    {
        if (deltaSeconds < 0 || double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds))
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        if (IsEnded)
        {
            RaiseFullTimeIfNeeded();
            return;
        }

        RemainingSeconds = Math.Max(0, RemainingSeconds - deltaSeconds);
        if (IsEnded)
        {
            RaiseFullTimeIfNeeded();
            Frame++;
            return;
        }

        if (restartPending)
        {
            AdvanceCleanRoomRestart(deltaSeconds);
            Frame++;
            return;
        }

        tackleProtectionSeconds = Math.Max(0, tackleProtectionSeconds - deltaSeconds);

        var step = (float)Math.Min(deltaSeconds, 0.25) * 0.55f;
        HomePosition = ClampPosition(
            HomePosition.X + input.MoveX * step,
            HomePosition.Y + input.MoveY * step);
        UpdateOpponentAi((float)deltaSeconds);

        var dx = HomePosition.X - BallPosition.X;
        var dy = HomePosition.Y - BallPosition.Y;
        var hasPossession = MathF.Sqrt(dx * dx + dy * dy) < PossessionRadius;
        var shootPressed = input.Shoot && !shootWasDown;
        var trickPressed = input.Trick && !trickWasDown;
        var pannaPressed = input.Panna && !pannaWasDown;
        var gamebreakerPressed = input.Gamebreaker && !gamebreakerWasDown;
        var jugglePressed = input.Juggle && !juggleWasDown;
        var tacklePressed = input.Tackle && !tackleWasDown;
        shootWasDown = input.Shoot;
        trickWasDown = input.Trick;
        pannaWasDown = input.Panna;
        gamebreakerWasDown = input.Gamebreaker;
        juggleWasDown = input.Juggle;
        tackleWasDown = input.Tackle;

        if (!jugglePressed && BallHeight > 0)
        {
            BallHeight = Math.Max(0, BallHeight - (float)Math.Min(deltaSeconds, 0.25) * 1.5f);
            if (BallHeight <= 0)
            {
                JuggleChain = 0;
            }
        }

        if (!ballInFlight)
        {
            UpdatePossession(hasPossession);
        }

        if (!ballInFlight && tacklePressed && Possession == BallOwner.Away &&
            DistanceTo(HomePosition.X, HomePosition.Y, AwayPosition) <= PossessionRadius)
        {
            // Authored clean-room defensive control. This is a new managed PC
            // rule and does not claim an original input, collision, animation,
            // audio or possession binding.
            TackleCount++;
            Possession = BallOwner.Home;
            homeHasTouchedBall = true;
            opponentPossessionSeconds = 0;
            tackleProtectionSeconds = 0.20;
            BallHeight = 0;
            JuggleChain = 0;
            AddGamebreakerMeter(10);
            SetAction("TACKLE");
        }

        if (Possession == BallOwner.Home)
        {
            homeHasTouchedBall = true;
        }

        if (!ballInFlight && gamebreakerPressed && Possession == BallOwner.Home &&
            GamebreakerMeter >= MaximumGamebreakerMeter)
        {
            // Authored clean-room mechanic. This is an opt-in PC rule with no
            // claimed original-game control, meter, animation or GPU binding.
            GamebreakerCount++;
            GamebreakerMeter = 0;
            ballInFlight = true;
            ballVelocityX = ShotSpeed * 1.25f;
            ballVelocityY = Math.Clamp(input.MoveY, -1.0f, 1.0f) * ShotSpeed * 0.55f;
            flightTarget = BallOwner.None;
            flightShooter = BallOwner.Home;
            Possession = BallOwner.None;
            BallHeight = 0;
            JuggleChain = 0;
            SetAction("GAMEBREAKER");
        }
        else if (!ballInFlight && input.Pass && Possession == BallOwner.Home)
        {
            BeginPass();
            SetAction("PASS");
        }
        else if (!ballInFlight && pannaPressed && Possession == BallOwner.Home)
        {
            PannaCount++;
            AddGamebreakerMeter(15);
            HomePosition = ClampPosition(HomePosition.X, HomePosition.Y + 0.04f);
            SetAction("PANNA");
            if (HasReachedTargetGoals)
            {
                RaiseFullTimeIfNeeded();
            }
        }
        else if (!ballInFlight && jugglePressed && Possession == BallOwner.Home)
        {
            // Authored clean-room ball-control mechanic. Its height, chain and
            // input are new PC behavior; no original frame or physics binding
            // is claimed.
            JuggleCount++;
            JuggleChain = Math.Min(JuggleChain + 1, 6);
            BallHeight = Math.Clamp(0.15f + JuggleChain * 0.05f, 0, MaximumBallHeight);
            AddGamebreakerMeter(5);
            SetAction("JUGGLE");
        }
        else if (!ballInFlight && trickPressed && Possession == BallOwner.Home)
        {
            TrickCount++;
            AddGamebreakerMeter(25);
            HomePosition = ClampPosition(HomePosition.X, HomePosition.Y + 0.06f);
            SetAction("TRICK");
        }
        else if (!ballInFlight && shootPressed && Possession == BallOwner.Home)
        {
            ballInFlight = true;
            ballVelocityX = ShotSpeed;
            // Clean-room control behavior: vertical input at shot time gives
            // the home player a deterministic aim component. The default demo
            // keeps MoveY at zero, so its straight-shot trace is unchanged.
            ballVelocityY = Math.Clamp(input.MoveY, -1.0f, 1.0f) * ShotSpeed * 0.55f;
            flightTarget = BallOwner.None;
            flightShooter = BallOwner.Home;
            Possession = BallOwner.None;
            BallHeight = 0;
            JuggleChain = 0;
            SetAction("SHOT");
        }

        if (ballInFlight)
        {
            var nextX = BallPosition.X + ballVelocityX * (float)deltaSeconds;
            var nextY = BallPosition.Y + ballVelocityY * (float)deltaSeconds;
            if (flightTarget == BallOwner.Away && DistanceTo(nextX, nextY, AwayPosition) <= PossessionRadius)
            {
                BallPosition = AwayPosition;
                ballInFlight = false;
                ballVelocityX = 0;
                ballVelocityY = 0;
                Possession = BallOwner.Away;
                AddGamebreakerMeter(10);
                SetAction("PASS_COMPLETE");
            }
            else if (flightTarget == BallOwner.None &&
                     ((flightShooter == BallOwner.Home && nextX >= 0.98f) ||
                      (flightShooter == BallOwner.Away && nextX <= 0.02f)))
            {
                var goalAction = flightShooter == BallOwner.Away ? "GOAL_AWAY" : "GOAL_HOME";
                if (flightShooter == BallOwner.Away)
                {
                    AwayScore++;
                }
                else
                {
                    HomeScore++;
                }
                BallPosition = (0.50f, 0.31f);
                ballInFlight = false;
                ballVelocityX = 0;
                ballVelocityY = 0;
                Possession = BallOwner.None;
                flightShooter = BallOwner.None;
                homeHasTouchedBall = false;
                BallHeight = 0;
                JuggleChain = 0;
                SetAction(goalAction);
                if (cleanRoomRoundResetEnabled && !HasReachedTargetGoals)
                {
                    BeginCleanRoomRestart(goalAction == "GOAL_AWAY" ? BallOwner.Away : BallOwner.Home);
                }
                if (HasReachedTargetGoals)
                {
                    RaiseFullTimeIfNeeded();
                }
            }
            else
            {
                BallPosition = ClampPosition(nextX, nextY);
            }
        }

        if (!ballInFlight)
        {
            SyncBallToPossessor();
        }

        Frame++;
    }

    public string HudText() =>
        $"HOME {HomeScore} - {AwayScore} AWAY   TIME {RemainingSeconds:000.0}";

    private void BeginPass()
    {
        var deltaX = AwayPosition.X - BallPosition.X;
        var deltaY = AwayPosition.Y - BallPosition.Y;
        var distance = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
        if (distance <= float.Epsilon)
        {
            Possession = BallOwner.Away;
            SetAction("PASS_COMPLETE");
            return;
        }

        ballInFlight = true;
        ballVelocityX = deltaX / distance * PassSpeed;
        ballVelocityY = deltaY / distance * PassSpeed;
        flightTarget = BallOwner.Away;
        flightShooter = BallOwner.Home;
        Possession = BallOwner.None;
        BallHeight = 0;
        JuggleChain = 0;
    }

    private void AddGamebreakerMeter(int amount)
    {
        GamebreakerMeter = Math.Clamp(GamebreakerMeter + amount, 0, MaximumGamebreakerMeter);
    }

    private void UpdatePossession(bool homeWithinRadius)
    {
        var awayWithinRadius = DistanceTo(BallPosition.X, BallPosition.Y, AwayPosition) < PossessionRadius;
        var previous = Possession;
        var awayWinsContest = opponentAiEnabled &&
            previous == BallOwner.Home &&
            awayWithinRadius &&
            DistanceTo(BallPosition.X, BallPosition.Y, AwayPosition) <= PossessionRadius * 0.45f &&
            tackleProtectionSeconds <= 0;
        if (opponentAiEnabled && previous == BallOwner.Away && awayWithinRadius)
        {
            Possession = BallOwner.Away;
        }
        else if (awayWinsContest)
        {
            Possession = BallOwner.Away;
        }
        else if (homeWithinRadius)
        {
            Possession = BallOwner.Home;
        }
        else if (awayWithinRadius)
        {
            Possession = BallOwner.Away;
        }
        else
        {
            Possession = BallOwner.None;
        }

        if (Possession == BallOwner.Away && previous != BallOwner.Away)
        {
            BallHeight = 0;
            JuggleChain = 0;
            opponentPossessionSeconds = 0;
            if (opponentAiEnabled && previous == BallOwner.Home)
            {
                SetAction("STEAL");
            }
        }
        else if (Possession != BallOwner.Away)
        {
            opponentPossessionSeconds = 0;
        }
    }

    private void UpdateOpponentAi(float deltaSeconds)
    {
        if (!opponentAiEnabled)
        {
            return;
        }

        if (Possession == BallOwner.Away && !ballInFlight)
        {
            BallPosition = AwayPosition;
            opponentPossessionSeconds += deltaSeconds;
            if (opponentPossessionSeconds >= OpponentShotDelaySeconds)
            {
                ballInFlight = true;
                ballVelocityX = -ShotSpeed;
                ballVelocityY = 0;
                flightTarget = BallOwner.None;
                flightShooter = BallOwner.Away;
                Possession = BallOwner.None;
                BallHeight = 0;
                JuggleChain = 0;
                opponentPossessionSeconds = 0;
                SetAction("SHOT_AWAY");
            }

            return;
        }

        if (ballInFlight || !homeHasTouchedBall)
        {
            return;
        }

        var target = Possession == BallOwner.Home ? HomePosition : BallPosition;
        AwayPosition = MoveToward(AwayPosition, target, OpponentPressSpeed * deltaSeconds);
    }

    private void SyncBallToPossessor()
    {
        BallPosition = Possession switch
        {
            BallOwner.Home => HomePosition,
            BallOwner.Away => AwayPosition,
            _ => BallPosition,
        };
    }

    private void SetAction(string action)
    {
        LastAction = action;
        ActionSequence++;
        actionCounts[action] = actionCounts.TryGetValue(action, out var count) ? count + 1 : 1;
        eventHistory.Add(new CleanRoomMatchEvent(
            Sequence: ActionSequence,
            Frame: Frame,
            Action: action,
            HomeScore: HomeScore,
            AwayScore: AwayScore,
            Possession: Possession));
    }

    private bool HasReachedTargetGoals => endCondition switch
    {
        CleanRoomMatchEndCondition.FirstToGoals =>
            HomeScore >= targetGoals || AwayScore >= targetGoals,
        CleanRoomMatchEndCondition.GoalsOrPanna =>
            HomeScore >= targetGoals || AwayScore >= targetGoals || PannaCount >= targetPannas,
        _ => false,
    };

    private void RaiseFullTimeIfNeeded()
    {
        if (fullTimeRaised)
        {
            return;
        }

        fullTimeRaised = true;
        Result = HomeScore == AwayScore
            ? CleanRoomMatchResult.Draw
            : HomeScore > AwayScore
                ? CleanRoomMatchResult.HomeWin
                : CleanRoomMatchResult.AwayWin;
        SetAction("FULL_TIME");
    }

    private static float DistanceTo(float x, float y, (float X, float Y) point)
    {
        var dx = x - point.X;
        var dy = y - point.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static (float X, float Y) ClampPosition(float x, float y) =>
        (Math.Clamp(x, 0.03f, FieldWidth - 0.03f), Math.Clamp(y, 0.03f, FieldHeight - 0.03f));

    private static (float X, float Y) MoveToward(
        (float X, float Y) origin,
        (float X, float Y) target,
        float distance)
    {
        var deltaX = target.X - origin.X;
        var deltaY = target.Y - origin.Y;
        var length = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
        if (length <= distance || length <= float.Epsilon)
        {
            return ClampPosition(target.X, target.Y);
        }

        var scale = distance / length;
        return ClampPosition(origin.X + deltaX * scale, origin.Y + deltaY * scale);
    }

    private void BeginCleanRoomRestart(BallOwner scorer)
    {
        // This is an authored clean-room round policy. It is deliberately not
        // presented as a decoded kickoff rule from the original game.
        HomePosition = (0.25f, 0.31f);
        AwayPosition = (0.75f, 0.31f);
        BallPosition = (0.50f, 0.31f);
        Possession = BallOwner.None;
        BallHeight = 0;
        JuggleChain = 0;
        restartOwner = scorer;
        restartSecondsRemaining = CleanRoomRestartDelaySeconds;
        restartPending = true;
        opponentPossessionSeconds = 0;
    }

    private void AdvanceCleanRoomRestart(double deltaSeconds)
    {
        restartSecondsRemaining = Math.Max(0, restartSecondsRemaining - deltaSeconds);
        if (restartSecondsRemaining > 0)
        {
            return;
        }

        restartPending = false;
        RoundNumber++;
        ballInFlight = false;
        ballVelocityX = 0;
        ballVelocityY = 0;
        flightTarget = BallOwner.None;
        flightShooter = BallOwner.None;
        BallHeight = 0;
        JuggleChain = 0;
        homeHasTouchedBall = restartOwner == BallOwner.Home;
        Possession = restartOwner;
        SyncBallToPossessor();
        SetAction("RESTART");
    }
}

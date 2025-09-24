using Common;
using System;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for automated work including ice cream mixer simulation.
    /// </summary>
    public class AutomationManager : IAutomationManager, IDisposable
    {
        private Thread automationWorker;
        private AutoResetEvent automationTrigger;
        private IStorage storage;
        private IProcessingManager processingManager;
        private int delayBetweenCommands;
        private IConfiguration configuration;

        // Ice cream mixer simulation state
        private enum MixerState
        {
            Idle,                   // Waiting for start signal
            FillingChocolate,       // Phase 1: Adding chocolate (100kg at 50kg/sec = 2 seconds)
            FillingMilk,           // Phase 2: Adding milk (150L at 50L/sec = 3 seconds)  
            FillingWater,          // Phase 3: Adding water (120L at 30L/sec = 4 seconds)
            Mixing,                // Phase 4: Mixing for 10 seconds
            Emptying,              // Phase 5: Emptying mixer (100kg/sec)
            Error                  // Error state - emergency stop
        }

        private MixerState currentState = MixerState.Idle;
        private int stateTimer = 0;
        private double mixerContents = 0.0; // Current amount in mixer (kg)

        // Point addresses from specification
        private const ushort START_ADDRESS = 3000;     // Start signal
        private const ushort MOTOR_ADDRESS = 3001;     // Motor control
        private const ushort VALVE_V1_ADDRESS = 4000;  // Chocolate valve
        private const ushort VALVE_V2_ADDRESS = 4001;  // Milk valve
        private const ushort VALVE_V3_ADDRESS = 4002;  // Water valve
        private const ushort VALVE_V4_ADDRESS = 4003;  // Drain valve
        private const ushort MIXER_CONTENTS_ADDRESS = 1000; // Analog output for contents

        // Flow rates from specification
        private const double CHOCOLATE_FLOW_RATE = 50.0; // kg/sec
        private const double MILK_FLOW_RATE = 50.0;      // L/sec (treating as kg/sec for simplicity)
        private const double WATER_FLOW_RATE = 30.0;     // L/sec (treating as kg/sec for simplicity)
        private const double DRAIN_FLOW_RATE = 100.0;    // kg/sec

        // Target amounts from specification
        private const double TARGET_CHOCOLATE = 100.0; // kg
        private const double TARGET_MILK = 150.0;       // L
        private const double TARGET_WATER = 120.0;      // L
        private const int MIXING_TIME = 10;             // seconds

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationManager"/> class.
        /// </summary>
        /// <param name="storage">The storage.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="automationTrigger">The automation trigger.</param>
        /// <param name="configuration">The configuration.</param>
        public AutomationManager(IStorage storage, IProcessingManager processingManager, AutoResetEvent automationTrigger, IConfiguration configuration)
        {
            this.storage = storage;
            this.processingManager = processingManager;
            this.configuration = configuration;
            this.automationTrigger = automationTrigger;
        }

        /// <summary>
        /// Initializes and starts the threads.
        /// </summary>
		private void InitializeAndStartThreads()
        {
            InitializeAutomationWorkerThread();
            StartAutomationWorkerThread();
        }

        /// <summary>
        /// Initializes the automation worker thread.
        /// </summary>
		private void InitializeAutomationWorkerThread()
        {
            automationWorker = new Thread(AutomationWorker_DoWork);
            automationWorker.Name = "Automation Thread";
        }

        /// <summary>
        /// Starts the automation worker thread.
        /// </summary>
		private void StartAutomationWorkerThread()
        {
            automationWorker.Start();
        }

        /// <summary>
        /// Main automation worker thread - implements ice cream mixer logic.
        /// </summary>
		private void AutomationWorker_DoWork()
        {
            while (!disposedValue)
            {
                try
                {
                    // Wait for timer signal (1 second intervals)
                    automationTrigger.WaitOne();

                    if (disposedValue) break;

                    // Get current point states
                    var startSignal = GetDigitalPointValue(START_ADDRESS);
                    var motorState = GetDigitalPointValue(MOTOR_ADDRESS);
                    var v1State = GetDigitalPointValue(VALVE_V1_ADDRESS);
                    var v2State = GetDigitalPointValue(VALVE_V2_ADDRESS);
                    var v3State = GetDigitalPointValue(VALVE_V3_ADDRESS);

                    // Safety check: If any ingredient valve opens during mixing, emergency stop
                    if (currentState == MixerState.Mixing && (v1State == 1 || v2State == 1 || v3State == 1))
                    {
                        EmergencyStop();
                        continue;
                    }

                    // State machine logic
                    switch (currentState)
                    {
                        case MixerState.Idle:
                            HandleIdleState(startSignal);
                            break;

                        case MixerState.FillingChocolate:
                            HandleFillingChocolateState();
                            break;

                        case MixerState.FillingMilk:
                            HandleFillingMilkState();
                            break;

                        case MixerState.FillingWater:
                            HandleFillingWaterState();
                            break;

                        case MixerState.Mixing:
                            HandleMixingState();
                            break;

                        case MixerState.Emptying:
                            HandleEmptyingState();
                            break;

                        case MixerState.Error:
                            HandleErrorState();
                            break;
                    }

                    // Update mixer contents display
                    UpdateMixerContents();

                    // Small delay to prevent excessive CPU usage
                    Thread.Sleep(delayBetweenCommands);
                }
                catch (Exception ex)
                {
                    // Log error and continue
                    Console.WriteLine($"Automation error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles the idle state - waiting for start signal.
        /// </summary>
        private void HandleIdleState(int startSignal)
        {
            if (startSignal == 1 && mixerContents == 0)
            {
                // Start the ice cream making process
                currentState = MixerState.FillingChocolate;
                stateTimer = 0;

                // Open chocolate valve
                SetDigitalPoint(VALVE_V1_ADDRESS, 1);

                // Ensure all other valves are closed and motor is off
                SetDigitalPoint(VALVE_V2_ADDRESS, 0);
                SetDigitalPoint(VALVE_V3_ADDRESS, 0);
                SetDigitalPoint(VALVE_V4_ADDRESS, 0);
                SetDigitalPoint(MOTOR_ADDRESS, 0);
            }
        }

        /// <summary>
        /// Handles chocolate filling phase.
        /// </summary>
        private void HandleFillingChocolateState()
        {
            stateTimer++;

            // Add chocolate to mixer
            mixerContents += CHOCOLATE_FLOW_RATE;

            // Check if we've added enough chocolate
            if (mixerContents >= TARGET_CHOCOLATE)
            {
                mixerContents = TARGET_CHOCOLATE; // Cap at target

                // Close chocolate valve, open milk valve
                SetDigitalPoint(VALVE_V1_ADDRESS, 0);
                SetDigitalPoint(VALVE_V2_ADDRESS, 1);

                currentState = MixerState.FillingMilk;
                stateTimer = 0;
            }
        }

        /// <summary>
        /// Handles milk filling phase.
        /// </summary>
        private void HandleFillingMilkState()
        {
            stateTimer++;

            // Add milk to mixer
            mixerContents += MILK_FLOW_RATE;

            // Check if we've added enough milk
            if (mixerContents >= TARGET_CHOCOLATE + TARGET_MILK)
            {
                mixerContents = TARGET_CHOCOLATE + TARGET_MILK; // Cap at target

                // Close milk valve, open water valve
                SetDigitalPoint(VALVE_V2_ADDRESS, 0);
                SetDigitalPoint(VALVE_V3_ADDRESS, 1);

                currentState = MixerState.FillingWater;
                stateTimer = 0;
            }
        }

        /// <summary>
        /// Handles water filling phase.
        /// </summary>
        private void HandleFillingWaterState()
        {
            stateTimer++;

            // Add water to mixer
            mixerContents += WATER_FLOW_RATE;

            // Check if we've added enough water
            if (mixerContents >= TARGET_CHOCOLATE + TARGET_MILK + TARGET_WATER)
            {
                mixerContents = TARGET_CHOCOLATE + TARGET_MILK + TARGET_WATER; // Cap at target

                // Close water valve, start motor
                SetDigitalPoint(VALVE_V3_ADDRESS, 0);
                SetDigitalPoint(MOTOR_ADDRESS, 1);

                currentState = MixerState.Mixing;
                stateTimer = 0;
            }
        }

        /// <summary>
        /// Handles mixing phase.
        /// </summary>
        private void HandleMixingState()
        {
            stateTimer++;

            // Mix for specified time
            if (stateTimer >= MIXING_TIME)
            {
                // Stop motor, open drain valve
                SetDigitalPoint(MOTOR_ADDRESS, 0);
                SetDigitalPoint(VALVE_V4_ADDRESS, 1);

                currentState = MixerState.Emptying;
                stateTimer = 0;
            }
        }

        /// <summary>
        /// Handles emptying phase.
        /// </summary>
        private void HandleEmptyingState()
        {
            stateTimer++;

            // Drain mixer contents
            mixerContents -= DRAIN_FLOW_RATE;

            if (mixerContents <= 0)
            {
                mixerContents = 0;

                // Close drain valve, reset start signal
                SetDigitalPoint(VALVE_V4_ADDRESS, 0);
                SetDigitalPoint(START_ADDRESS, 0);

                currentState = MixerState.Idle;
                stateTimer = 0;
            }
        }

        /// <summary>
        /// Handles error state.
        /// </summary>
        private void HandleErrorState()
        {
            // Stay in error state until manually reset
            // Could be enhanced to auto-reset after some time
        }

        /// <summary>
        /// Emergency stop procedure - close all ingredient valves, stop motor, empty mixer.
        /// </summary>
        private void EmergencyStop()
        {
            // Stop motor immediately
            SetDigitalPoint(MOTOR_ADDRESS, 0);

            // Close all ingredient valves
            SetDigitalPoint(VALVE_V1_ADDRESS, 0);
            SetDigitalPoint(VALVE_V2_ADDRESS, 0);
            SetDigitalPoint(VALVE_V3_ADDRESS, 0);

            // Open drain valve to empty mixer
            SetDigitalPoint(VALVE_V4_ADDRESS, 1);

            currentState = MixerState.Emptying;
            stateTimer = 0;
        }

        /// <summary>
        /// Gets the current value of a digital point.
        /// </summary>
        private int GetDigitalPointValue(ushort address)
        {
            try
            {
                var points = storage.GetPoints(new System.Collections.Generic.List<PointIdentifier>
                {
                    new PointIdentifier(PointType.DIGITAL_OUTPUT, address)
                });

                if (points.Count > 0)
                {
                    return points[0].RawValue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading point {address}: {ex.Message}");
            }

            return 0; // Default to off/closed
        }

        /// <summary>
        /// Sets the value of a digital point.
        /// </summary>
        private void SetDigitalPoint(ushort address, int value)
        {
            try
            {
                // Find the config item for this address
                var configItems = configuration.GetConfigurationItems();
                var configItem = configItems.Find(item =>
                    item.StartAddress <= address &&
                    address < item.StartAddress + item.NumberOfRegisters &&
                    (item.RegistryType == PointType.DIGITAL_OUTPUT || item.RegistryType == PointType.DIGITAL_INPUT));

                if (configItem != null)
                {
                    processingManager.ExecuteWriteCommand(
                        configItem,
                        configuration.GetTransactionId(),
                        configuration.UnitAddress,
                        address,
                        value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing point {address}: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the mixer contents analog output.
        /// </summary>
        private void UpdateMixerContents()
        {
            try
            {
                // Find the analog output config item for mixer contents
                var configItems = configuration.GetConfigurationItems();
                var configItem = configItems.Find(item =>
                    item.StartAddress <= MIXER_CONTENTS_ADDRESS &&
                    MIXER_CONTENTS_ADDRESS < item.StartAddress + item.NumberOfRegisters &&
                    item.RegistryType == PointType.ANALOG_OUTPUT);

                if (configItem != null)
                {
                    // Convert EGU value (kg) to raw value using inverse EGU formula
                    // raw_value = (EGU_value - B) / A
                    var eguConverter = new EGUConverter();
                    ushort rawValue = eguConverter.ConvertToRaw(configItem.ScaleFactor, configItem.Deviation, mixerContents);

                    processingManager.ExecuteWriteCommand(
                        configItem,
                        configuration.GetTransactionId(),
                        configuration.UnitAddress,
                        MIXER_CONTENTS_ADDRESS,
                        rawValue);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating mixer contents: {ex.Message}");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Indication if managed objects should be disposed.</param>
		protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Ensure all valves are closed and motor is stopped on disposal
                    try
                    {
                        SetDigitalPoint(VALVE_V1_ADDRESS, 0);
                        SetDigitalPoint(VALVE_V2_ADDRESS, 0);
                        SetDigitalPoint(VALVE_V3_ADDRESS, 0);
                        SetDigitalPoint(VALVE_V4_ADDRESS, 0);
                        SetDigitalPoint(MOTOR_ADDRESS, 0);
                    }
                    catch
                    {
                        // Ignore errors during disposal
                    }
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        /// <inheritdoc />
        public void Start(int delayBetweenCommands)
        {
            this.delayBetweenCommands = delayBetweenCommands * 1000;
            InitializeAndStartThreads();
        }

        /// <inheritdoc />
        public void Stop()
        {
            Dispose();
        }
        #endregion
    }
}
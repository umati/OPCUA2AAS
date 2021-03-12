
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Station
{
    public enum StationStatus : int
    {
        Ready = 0,
        WorkInProgress = 1,
        Fault = 2
    }

    public partial class StationState
    {
        private Timer m_stationClock;
        private ISystemContext m_context;

        protected override void OnAfterCreate(ISystemContext context, NodeState node)
        {
            base.OnAfterCreate(context, node);

            StationState objectNode = (StationState) node;

            //if (m_stationTelemetry.OverallRunningTime == null)
            //{
            //    m_stationTelemetry.OverallRunningTime = new BaseDataVariableState<ulong>(m_stationTelemetry);
            //    m_stationTelemetry.OverallRunningTime.TypeDefinitionId = Variables.StationInstance_StationTelemetry_OverallRunningTime;
            //    m_stationTelemetry.OverallRunningTime.Value = 0;
            //}

            //if (m_stationTelemetry.FaultyTime == null)
            //{
            //    m_stationTelemetry.FaultyTime = new BaseDataVariableState<ulong>(m_stationTelemetry);
            //    m_stationTelemetry.FaultyTime.TypeDefinitionId = Variables.StationInstance_StationTelemetry_FaultyTime;
            //    m_stationTelemetry.FaultyTime.Value = 0;
            //}

            //if (m_stationTelemetry.IdealCycleTime == null)
            //{
            //    m_stationTelemetry.IdealCycleTime = new BaseDataVariableState<ulong>(m_stationTelemetry);
            //    m_stationTelemetry.FaultyTime.TypeDefinitionId = Variables.StationInstance_StationTelemetry_IdealCycleTime;
            //    m_stationTelemetry.IdealCycleTime.Value = 5000;
            //}

            //if (m_stationTelemetry.ActualCycleTime == null)
            //{
            //    m_stationTelemetry.ActualCycleTime = new BaseDataVariableState<ulong>(m_stationTelemetry);
            //    m_stationTelemetry.ActualCycleTime.TypeDefinitionId = Variables.StationInstance_StationTelemetry_ActualCycleTime;
            //    m_stationTelemetry.ActualCycleTime.Value = m_stationTelemetry.IdealCycleTime.Value;
            //}

            //if (m_stationTelemetry.Status == null)
            //{
            //    m_stationTelemetry.Status = new BaseDataVariableState<int>(m_stationTelemetry);
            //    m_stationTelemetry.Status.TypeDefinitionId = Variables.StationInstance_StationTelemetry_Status;
            //    m_stationTelemetry.Status.Value = StationStatus.Ready;
            //}

            //if (m_stationTelemetry.EnergyConsumption == null)
            //{
            //    m_stationTelemetry.EnergyConsumption = new BaseDataVariableState<double>(m_stationTelemetry);
            //    m_stationTelemetry.EnergyConsumption.TypeDefinitionId = Variables.StationInstance_StationTelemetry_EnergyConsumption;
            //    m_stationTelemetry.EnergyConsumption.Value = 1000;
            //}

            //if (m_stationTelemetry.Pressure == null)
            //{
            //    m_stationTelemetry.Pressure = new BaseDataVariableState<double>(m_stationTelemetry);
            //    m_stationTelemetry.Pressure.TypeDefinitionId = Variables.StationInstance_StationTelemetry_Pressure;
            //    m_stationTelemetry.Pressure.Value = 1000;
            //}

            //if (m_stationProduct == null)
            //{
            //    m_stationProduct = new StationProductState(this);
            //    m_stationProduct.TypeDefinitionId = Objects.StationInstance_StationProduct;
            //}

            //if (m_stationProduct.ProductSerialNumber == null)
            //{
            //    m_stationProduct.ProductSerialNumber = new BaseDataVariableState<ulong>(m_stationProduct);
            //    m_stationProduct.ProductSerialNumber.TypeDefinitionId = Variables.StationInstance_StationProduct_ProductSerialNumber;
            //    m_stationProduct.ProductSerialNumber.Value = 0;
            //}

            //if (m_stationProduct.NumberOfManufacturedProducts == null)
            //{
            //    m_stationProduct.NumberOfManufacturedProducts = new BaseDataVariableState<ulong>(m_stationProduct);
            //    m_stationProduct.NumberOfManufacturedProducts.TypeDefinitionId = Variables.StationInstance_StationProduct_NumberOfManufacturedProducts;
            //    m_stationProduct.NumberOfManufacturedProducts.Value = 0;
            //}

            //if (m_stationProduct.NumberOfDiscardedProducts == null)
            //{
            //    m_stationProduct.NumberOfDiscardedProducts = new BaseDataVariableState<ulong>(m_stationProduct);
            //    m_stationProduct.NumberOfDiscardedProducts.TypeDefinitionId = Variables.StationInstance_StationProduct_NumberOfDiscardedProducts;
            //    m_stationProduct.NumberOfDiscardedProducts.Value = 0;
            //}

            //if (StationCommands == null)
            //{
            //    StationCommands = new StationCommandsState(this);
            //    StationCommands.TypeDefinitionId = Objects.StationInstance_StationCommands;
            //}

            //if (StationCommands.Execute == null)
            //{
            //    StationCommands.Execute = new ExecuteMethodState(StationCommands);
            //    StationCommands.Execute.TypeDefinitionId = Methods.StationInstance_StationCommands_Execute;
            //    StationCommands.Execute.OnCallMethod = Execute;
            //}

            //if (StationCommands.Reset == null)
            //{
            //    StationCommands.Reset = new MethodState(StationCommands);
            //    StationCommands.Reset.TypeDefinitionId = Methods.StationInstance_StationCommands_Reset;
            //    StationCommands.Reset.OnCallMethod = Reset;
            //}

            //if (StationCommands.OpenPressureReleaseValve == null)
            //{
            //    StationCommands.OpenPressureReleaseValve = new MethodState(StationCommands);
            //    StationCommands.OpenPressureReleaseValve.TypeDefinitionId = Methods.StationInstance_StationCommands_OpenPressureReleaseValve;
            //    StationCommands.OpenPressureReleaseValve.OnCallMethod = OpenPressureReleaseValve;
            //}

            m_context = context;
            //m_stationClock = new Timer(Tick, this, Timeout.Infinite, (int)m_stationTelemetry.ActualCycleTime.Value);
        }

        private ServiceResult Execute(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if ((int)m_stationTelemetry.Status.Value == (int)StationStatus.Fault)
            {
                ServiceResult result = new ServiceResult(new Exception("Machine is in fault state, call reset first!"));
                return result;
            }

            m_stationProduct.ProductSerialNumber.Value = (ulong)inputArguments[0];

            m_stationTelemetry.Status.Value = StationStatus.WorkInProgress;

            m_stationClock.Change((int)m_stationTelemetry.ActualCycleTime.Value, (int)m_stationTelemetry.ActualCycleTime.Value);

            return ServiceResult.Good;
        }

        private ServiceResult Reset(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_stationTelemetry.Status.Value = StationStatus.Ready;

            return ServiceResult.Good;
        }

        private ServiceResult OpenPressureReleaseValve(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_stationTelemetry.Pressure.Value = 1000;

            return ServiceResult.Good;
        }

        private static void Tick(object state)
        {
            ((StationState)state).UpdateNodeValues();
        }

        private void UpdateNodeValues()
        {
            if ((int)m_stationTelemetry.Status.Value == (int)StationStatus.Fault)
            {
                return;
            }

            // we produce a discarded product every 100 parts
            // we go into fault mode every 1000 parts
            if ((m_stationProduct.NumberOfManufacturedProducts.Value % 1000) == 0)
            {
                m_stationTelemetry.Status.Value = StationStatus.Fault;
            }
            else if ((m_stationProduct.NumberOfManufacturedProducts.Value % 100) == 0)
            {
                m_stationProduct.NumberOfDiscardedProducts.Value++;
            }
            else
            {
                m_stationProduct.NumberOfManufacturedProducts.Value++;
            }

            // update source timestamps
            List<BaseInstanceState> m_telemetryList = new List<BaseInstanceState>();
            m_stationProduct.GetChildren(m_context, m_telemetryList);
            m_stationTelemetry.GetChildren(m_context, m_telemetryList);

            foreach (BaseInstanceState children in m_telemetryList)
            {
                if ((children.ChangeMasks & NodeStateChangeMasks.Value) != 0)
                {
                    BaseDataVariableState dataVariable = children as BaseDataVariableState;
                    if (dataVariable != null)
                    {
                        dataVariable.Timestamp = DateTime.Now;
                    }
                }
            }

            ClearChangeMasks(m_context, true);
        }
    }
}

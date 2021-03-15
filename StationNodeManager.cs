
using Opc.Ua.Export;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Opc.Ua.Sample
{
    public enum StationStatus : int
    {
        Ready = 0,
        WorkInProgress = 1,
        Fault = 2
    }

    public class StationNodeManager : CustomNodeManager2
    {
        private Timer m_stationClock;

        private static ulong m_overallRunningTime = 0;
        private static ulong m_faultyTime = 0;
        private static ulong m_idealCycleTime = 5000;
        private static ulong m_actualCycleTime = 5000;
        private static StationStatus m_status = StationStatus.Ready;
        private static ulong m_energyConsumption = 1000;
        private static ulong m_pressure = 1000;
        private static ulong m_productSerialNumber = 1;
        private static ulong m_numberOfManufacturedProducts = 1;
        private static ulong m_numberOfDiscardedProducts = 0;

        private ushort m_namespaceIndex;
        private long m_lastUsedId;
        private static StationNodeManager m_this;

        public StationNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        : base(server, configuration)
        {
            SystemContext.NodeIdFactory = this;

            List<string> namespaceUris = new List<string>();
            namespaceUris.Add("http://opcfoundation.org/UA/Station/");
            NamespaceUris = namespaceUris;

            m_namespaceIndex = Server.NamespaceUris.GetIndexOrAppend(namespaceUris[0]);
            m_lastUsedId = 0;
            m_stationClock = new Timer(Tick, this, Timeout.Infinite, (int)m_actualCycleTime);
            m_this = this;
        }

        public override NodeId New(ISystemContext context, NodeState node)
        {
            uint id = Utils.IncrementIdentifier(ref m_lastUsedId);
            return new NodeId(id, m_namespaceIndex);
        }

        private FolderState CreateFolder(NodeState parent, string path, string name)
        {
            FolderState folder = new FolderState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.None
            };
            parent?.AddChild(folder);

            return folder;
        }

        private MethodState CreateMethod(NodeState parent, string path, string name)
        {
            MethodState method = new MethodState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                Executable = true,
                UserExecutable = true
            };

            parent?.AddChild(method);

            return method;
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                IList<IReference> references = null;
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                ImportNodeset2Xml(externalReferences, "Station.NodeSet2.xml");

                FolderState root = CreateFolder(null, "AssetAdminShell", "AssetAdminShell");
                root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, root.NodeId));
                root.EventNotifier = EventNotifiers.SubscribeToEvents;
                AddRootNotifier(root);

                MethodState createAASMethod = CreateMethod(root, "GenerateAAS", "GenerateAAS");
                createAASMethod.OnCallMethod = new GenericMethodCalledEventHandler(OnGenerateAASCall);

                AddPredefinedNode(SystemContext, root);

                AddReverseReferences(externalReferences);
            }
        }

        private ServiceResult OnGenerateAASCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            // TODO
            return ServiceResult.Good;
        }

        private void ImportNodeset2Xml(IDictionary<NodeId, IList<IReference>> externalReferences, string resourcepath)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();

            Stream stream = new FileStream(resourcepath, FileMode.Open);
            UANodeSet nodeSet = UANodeSet.Read(stream);

            foreach (string namespaceUri in nodeSet.NamespaceUris)
            {
                SystemContext.NamespaceUris.GetIndexOrAppend(namespaceUri);
            }
            nodeSet.Import(SystemContext, predefinedNodes);

            for (int i = 0; i < predefinedNodes.Count; i++)
            {
                AddPredefinedNode(SystemContext, predefinedNodes[i]);
            }
        }

        protected override NodeState AddBehaviourToPredefinedNode(ISystemContext context, NodeState predefinedNode)
        {
            MethodState methodState = predefinedNode as MethodState;
            if (methodState != null)
            {
                if (methodState.DisplayName == "Execute")
                {
                    methodState.OnCallMethod = new GenericMethodCalledEventHandler(Execute);

                    // define the method's input argument (the serial number)
                    methodState.InputArguments = new PropertyState<Argument[]>(methodState)
                    {
                        NodeId = new NodeId(methodState.BrowseName.Name + "InArgs", NamespaceIndex),
                        BrowseName = BrowseNames.InputArguments
                    };
                    methodState.InputArguments.DisplayName = methodState.InputArguments.BrowseName.Name;
                    methodState.InputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                    methodState.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                    methodState.InputArguments.DataType = DataTypeIds.Argument;
                    methodState.InputArguments.ValueRank = ValueRanks.OneDimension;

                    methodState.InputArguments.Value = new Argument[]
                    {
                        new Argument { Name = "SerialNumber", Description = "Serial number of the product to make.",  DataType = DataTypeIds.UInt64, ValueRank = ValueRanks.Scalar }
                    };

                    return predefinedNode;
                }
                if (methodState.DisplayName == "Reset")
                {
                    methodState.OnCallMethod = new GenericMethodCalledEventHandler(Reset);
                    return predefinedNode;
                }
                if (methodState.DisplayName == "OpenPressureReleaseValve")
                {
                    methodState.OnCallMethod = new GenericMethodCalledEventHandler(OpenPressureReleaseValve);
                    return predefinedNode;
                }
            }

            return predefinedNode;
        }

        private ServiceResult Execute(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (m_status == StationStatus.Fault)
            {
                ServiceResult result = new ServiceResult(new Exception("Machine is in fault state, call reset first!"));
                return result;
            }

            m_productSerialNumber = (ulong)inputArguments[0];

            m_status = StationStatus.WorkInProgress;

            m_stationClock.Change((int)m_actualCycleTime, (int)m_actualCycleTime);

            return ServiceResult.Good;
        }

        private ServiceResult Reset(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_status = StationStatus.Ready;

            return ServiceResult.Good;
        }

        private ServiceResult OpenPressureReleaseValve(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_pressure = 1000;

            return ServiceResult.Good;
        }

        private static void UpdateVariables()
        {
            NodeState node = m_this.Find(new NodeId(Station.VariableIds.StationInstance_StationProduct_NumberOfManufacturedProducts.Identifier, m_this.m_namespaceIndex));
            BaseDataVariableState variableState = node as BaseDataVariableState;
            if (variableState != null)
            {
                variableState.Value = m_numberOfManufacturedProducts;
            }

            node = m_this.Find(new NodeId(Station.VariableIds.StationInstance_StationProduct_NumberOfDiscardedProducts.Identifier, m_this.m_namespaceIndex));
            variableState = node as BaseDataVariableState;
            if (variableState != null)
            {
                variableState.Value = m_numberOfDiscardedProducts;
            }

            node = m_this.Find(new NodeId(Station.VariableIds.StationInstance_StationProduct_ProductSerialNumber.Identifier, m_this.m_namespaceIndex));
            variableState = node as BaseDataVariableState;
            if (variableState != null)
            {
                variableState.Value = m_productSerialNumber;
            }

            node = m_this.Find(new NodeId(Station.VariableIds.StationInstance_StationTelemetry_ActualCycleTime.Identifier, m_this.m_namespaceIndex));
            variableState = node as BaseDataVariableState;
            if (variableState != null)
            {
                variableState.Value = m_actualCycleTime;
            }

            node = m_this.Find(new NodeId(Station.VariableIds.StationInstance_StationTelemetry_EnergyConsumption.Identifier, m_this.m_namespaceIndex));
            variableState = node as BaseDataVariableState;
            if (variableState != null)
            {
                variableState.Value = m_energyConsumption;
            }

            node = m_this.Find(new NodeId(Station.VariableIds.StationInstance_StationTelemetry_FaultyTime.Identifier, m_this.m_namespaceIndex));
            variableState = node as BaseDataVariableState;
            if (variableState != null)
            {
                variableState.Value = m_faultyTime;
            }

            node = m_this.Find(new NodeId(Station.VariableIds.StationInstance_StationTelemetry_IdealCycleTime.Identifier, m_this.m_namespaceIndex));
            variableState = node as BaseDataVariableState;
            if (variableState != null)
            {
                variableState.Value = m_idealCycleTime;
            }

            node = m_this.Find(new NodeId(Station.VariableIds.StationInstance_StationTelemetry_OverallRunningTime.Identifier, m_this.m_namespaceIndex));
            variableState = node as BaseDataVariableState;
            if (variableState != null)
            {
                variableState.Value = m_overallRunningTime;
            }

            node = m_this.Find(new NodeId(Station.VariableIds.StationInstance_StationTelemetry_Pressure.Identifier, m_this.m_namespaceIndex));
            variableState = node as BaseDataVariableState;
            if (variableState != null)
            {
                variableState.Value = m_pressure;
            }

            node = m_this.Find(new NodeId(Station.VariableIds.StationInstance_StationTelemetry_Status.Identifier, m_this.m_namespaceIndex));
            variableState = node as BaseDataVariableState;
            if (variableState != null)
            {
                variableState.Value = m_status;
            }
        }

        private static void Tick(object state)
        {
            if (m_status == StationStatus.Fault)
            {
                return;
            }

            if (m_status == StationStatus.WorkInProgress)
            {
                // we produce a discarded product every 100 parts
                // we go into fault mode every 1000 parts
                if ((m_numberOfManufacturedProducts % 1000) == 0)
                {
                    m_status = StationStatus.Fault;
                }
                else if ((m_numberOfManufacturedProducts % 100) == 0)
                {
                    m_numberOfDiscardedProducts++;
                    m_productSerialNumber++;
                }
                else
                {
                    m_numberOfManufacturedProducts++;
                    m_productSerialNumber++;
                }
            }

            UpdateVariables();
        }
    }
}

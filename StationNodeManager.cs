
using Opc.Ua.Export;
using Opc.Ua.Server;
using System.Collections.Generic;
using System.IO;

namespace Opc.Ua.Sample
{
    public class StationNodeManager : CustomNodeManager2
    {
        public StationNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        : base(server, configuration)
        {
            SystemContext.NodeIdFactory = this;

            List<string> namespaceUris = new List<string>();
            namespaceUris.Add("http://opcfoundation.org/UA/Station/");
            NamespaceUris = namespaceUris;

            m_namespaceIndex = Server.NamespaceUris.GetIndexOrAppend(namespaceUris[0]);
            m_lastUsedId = 0;
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
            BaseObjectState objectNode = predefinedNode as BaseObjectState;
            if (objectNode == null)
            {
                return predefinedNode;
            }

            NodeId typeId = objectNode.TypeDefinitionId;
            if (!IsNodeIdInNamespace(typeId) || typeId.IdType != IdType.Numeric)
            {
                return predefinedNode;
            }

            switch ((uint)typeId.Identifier)
            {
                case Station.ObjectTypes.StationType:
                {
                    if (objectNode is Station.StationState)
                    {
                        break;
                    }

                    //    Station.StationState newNode = new Station.StationState(objectNode.Parent);
                    //    newNode.Create(context, objectNode);

                    //    objectNode.Parent?.ReplaceChild(context, newNode);

                    //    return newNode;
                    break;
                }
            }

            return predefinedNode;
        }

        private ushort m_namespaceIndex;
        private long m_lastUsedId;
    }
}

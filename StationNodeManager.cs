
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
            //namespaceUris.Add("http://opcfoundation.org/UA/Station/Instance");
            NamespaceUris = namespaceUris;

            m_namespaceIndex = Server.NamespaceUris.GetIndexOrAppend(namespaceUris[0]);
            m_lastUsedId = 0;
        }

        public override NodeId New(ISystemContext context, NodeState node)
        {
            uint id = Utils.IncrementIdentifier(ref m_lastUsedId);
            return new NodeId(id, m_namespaceIndex);
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
            }
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

            for (int ii = 0; ii < predefinedNodes.Count; ii++)
            {
                AddPredefinedNode(SystemContext, predefinedNodes[ii]);
            }

            AddReverseReferences(externalReferences);
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

                        Station.StationState newNode = new Station.StationState(objectNode.Parent);
                        newNode.Create(context, objectNode);

                        // replace the node in the parent.
                        if (objectNode.Parent != null)
                        {
                            objectNode.Parent.ReplaceChild(context, newNode);
                        }

                        return newNode;
                    }

                case Station.ObjectTypes.StationProductType:
                    {
                        if (objectNode is Station.StationProductState)
                        {
                            break;
                        }

                        Station.StationProductState newNode = new Station.StationProductState(objectNode.Parent);
                        newNode.Create(context, objectNode);

                        // replace the node in the parent.
                        if (objectNode.Parent != null)
                        {
                            objectNode.Parent.ReplaceChild(context, newNode);
                        }

                        return newNode;
                    }

                case Station.ObjectTypes.StationCommandsType:
                    {
                        if (objectNode is Station.StationCommandsState)
                        {
                            break;
                        }

                        Station.StationCommandsState newNode = new Station.StationCommandsState(objectNode.Parent);
                        newNode.Create(context, objectNode);

                        // replace the node in the parent.
                        if (objectNode.Parent != null)
                        {
                            objectNode.Parent.ReplaceChild(context, newNode);
                        }

                        return newNode;
                    }

                case Station.ObjectTypes.TelemetryType:
                    {
                        if (objectNode is Station.TelemetryState)
                        {
                            break;
                        }

                        Station.TelemetryState newNode = new Station.TelemetryState(objectNode.Parent);
                        newNode.Create(context, objectNode);

                        // replace the node in the parent.
                        if (objectNode.Parent != null)
                        {
                            objectNode.Parent.ReplaceChild(context, newNode);
                        }

                        return newNode;
                    }
            }

            return predefinedNode;
        }

        private ushort m_namespaceIndex;
        private long m_lastUsedId;
    }
}

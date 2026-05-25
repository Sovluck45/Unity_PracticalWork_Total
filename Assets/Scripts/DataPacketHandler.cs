using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class DataPacketHandler : NetworkBehaviour
{
    [SyncVar] Vector3 syncPosition;

    public void SendPosition(Vector3 position)
    {
        if (!isLocalPlayer)
            return;
        CmdUpdatePosition(position);
    }

    [Command]
    void CmdUpdatePosition(Vector3 position)
    {
        syncPosition = position;
        RpcReceivePosition(position);
    }

    [ClientRpc]
    void RpcReceivePosition(Vector3 position)
    {
        if (isLocalPlayer)
            return;
        syncPosition = position;
        transform.position = position;
    }

    void LateUpdate()
    {
        if (isLocalPlayer || syncPosition == Vector3.zero)
            return;
        transform.position = Vector3.Lerp(transform.position, syncPosition, Time.deltaTime * 12f);
    }
}

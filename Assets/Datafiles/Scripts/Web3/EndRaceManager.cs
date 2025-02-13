using System.Collections;
using System.Collections.Generic;
using Aptos.Accounts;
using Aptos.BCS;
using Aptos.HdWallet.Utils;
using Aptos.Unity.Rest;
using Aptos.Unity.Rest.Model;
using Photon.Pun;
using UnityEngine;

public class EndRaceManager : MonoBehaviourPunCallbacks
{
    public IEnumerator OnEndRace(ulong raceId, List<BString> winningOrder)
    {
        yield return new WaitForSeconds(5);
        ResponseInfo responseInfo = new();
        
        byte[] bytes = "3e79e6c4f4d55299f09b3aef9a8ba33a2ba0f53d081336c3811c3e4712a8d48b".ByteArrayFromHexString();
        Sequence sequence = new(new ISerializable[] 
        { 
            new U64(raceId), 
            winningOrder[0], 
            winningOrder[1], 
            winningOrder[2], 
            winningOrder[3], 
            winningOrder[4] 
        });

        var payload = new EntryFunction
        (
            new(new AccountAddress(bytes), "aptos_horses_game"),
            "on_race_end",
            new(new ISerializableTag[] { }),
            sequence
        );

        Transaction buyTx = new();
        Coroutine buy = StartCoroutine(RestClient.Instance.SubmitTransaction((_transaction, _responseInfo) =>
        {
            buyTx = _transaction;
            responseInfo = _responseInfo;
        },
        WalletManager.Instance.Wallet.Account,
        payload));
        yield return buy;

        if (responseInfo.status != ResponseInfo.Status.Success)
        {
            Debug.LogError("Cannot distribute rewards. " + responseInfo.message);
            yield break;
        }

        Debug.Log("Transaction: " + buyTx.Hash);
        Coroutine waitForTransactionCor = StartCoroutine(RestClient.Instance.WaitForTransaction((_pending, _responseInfo) => 
        { 
            responseInfo = _responseInfo; 
        }, buyTx.Hash));
        yield return waitForTransactionCor;

        Debug.Log(responseInfo.status);

        if (responseInfo.status == ResponseInfo.Status.Success)
        {
            //Success
            WalletManager.Instance._completedRace = true;
            RaceManager.instance._completedRace = true;
            PlayerPrefs.SetInt("CompletedRace", 1);
            //Switch back to main scene and leave the race for all players
        }
        else Debug.Log(responseInfo.message);
    }
}
using System.Linq;
using App.Common.Data.MasterData;
using UnityEngine;

namespace App.Common.Data.Database
{
    [CreateAssetMenu(fileName = "BuffDatabase", menuName = "Database/BuffDatabase")]
    public class BuffDatabase : ScriptableObject
    {
        [SerializeField] private BuffMasterData[] _buffMasterData;

        public bool TryGetBuffMasterData(string id, out BuffMasterData buffMasterData)
        {
            buffMasterData = _buffMasterData.FirstOrDefault(d => d.Id == id);
            return buffMasterData != null;
        }
    }
}

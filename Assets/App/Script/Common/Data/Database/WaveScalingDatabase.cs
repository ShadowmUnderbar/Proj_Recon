using App.Common.Data.MasterData;
using UnityEngine;

namespace App.Common.Data.Database
{
    [CreateAssetMenu(fileName = "WaveScalingDatabase", menuName = "Database/WaveScalingDatabase")]
    public class WaveScalingDatabase : ScriptableObject
    {
        [SerializeField] private WaveScalingMasterData[] _waveScalingMasterData;

        public WaveScalingMasterData[] WaveScalingMasterData => _waveScalingMasterData;
    }
}

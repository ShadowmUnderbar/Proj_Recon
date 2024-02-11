using App.Battle.Interface.Views;
using UnityEngine;

namespace App.Battle.Views
{
    public class TestBullet : MonoBehaviour ,ITestBullet
    {
        [SerializeField]
        private float _speed = 3f; 
        public void Spawn(Pose pose)
        {
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }
        private void Update()
        {
            transform.position += transform.forward * _speed;
        }
    }
}
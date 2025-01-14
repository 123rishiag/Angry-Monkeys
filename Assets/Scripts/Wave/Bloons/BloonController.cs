using System.Collections.Generic;
using UnityEngine;
using ServiceLocator.Main;
using ServiceLocator.Player;
using ServiceLocator.Sound;
using ServiceLocator.Utilities;

namespace ServiceLocator.Wave.Bloon
{
    public class BloonController
    {
        private PlayerService playerService;
        private SoundService soundService;
        private WaveService waveService;

        private BloonView bloonView;
        private BloonScriptableObject bloonScriptableObject;

        private const float waypointThreshold = 0.1f;
        private List<Vector3> waypoints;
        private int currentHealth;
        private int currentWaypointIndex;
        private BloonState currentState;

        public Vector3 Position => bloonView.transform.position;

        public BloonController(PlayerService playerService, WaveService waveService, SoundService soundService, BloonView bloonPrefab, Transform bloonContainer)
        {
            this.playerService = playerService;
            this.soundService = soundService;
            this.waveService = waveService;

            bloonView = Object.Instantiate(bloonPrefab, bloonContainer);
            bloonView.Controller = this;
        }

        public void Init(BloonScriptableObject bloonScriptableObject)
        {
            this.bloonScriptableObject = bloonScriptableObject;
            InitializeVariables();
            SetState(BloonState.ACTIVE);
        }

        private void InitializeVariables()
        {
            bloonView.SetRenderer(bloonScriptableObject.Sprite);
            currentHealth = bloonScriptableObject.Health;
            waypoints = new List<Vector3>();
        }

        public void SetPosition(Vector3 spawnPosition)
        {
            bloonView.transform.position = spawnPosition;
            bloonView.gameObject.SetActive(true);
        }

        public void SetWayPoints(List<Vector3> waypointsToSet, int startingWaypointIndex)
        {
            waypoints = waypointsToSet;
            currentWaypointIndex = startingWaypointIndex;
        }

        public void TakeDamage(int damageToTake)
        {
            int reducedHealth = currentHealth - damageToTake;
            currentHealth = reducedHealth <= 0 ? 0 : reducedHealth;

            if (currentHealth <= 0 && currentState == BloonState.ACTIVE)
            {
                PopBloon();
                soundService.PlaySoundEffects(Sound.SoundType.BloonPop);
            }
        }

        public void FollowWayPoints()
        {
            if(HasReachedFinalWaypoint())
            {
                ResetBloon();
            }
            else
            {
                Vector3 direction = GetDirectionToMoveTowards();
                MoveBloon(direction);
                if (HasReachedNextWaypoint(direction.magnitude))
                    currentWaypointIndex++;
            }
        }

        private bool HasReachedFinalWaypoint() => currentWaypointIndex == waypoints.Count;

        private bool HasReachedNextWaypoint(float distance) => distance < waypointThreshold;

        private void ResetBloon()
        {
            waveService.RemoveBloon(this);
            playerService.TakeDamage(bloonScriptableObject.Damage);
            bloonView.gameObject.SetActive(false);
        }

        private Vector3 GetDirectionToMoveTowards() => waypoints[currentWaypointIndex] - bloonView.transform.position;

        private void MoveBloon(Vector3 moveDirection) => bloonView.transform.Translate(moveDirection.normalized * bloonScriptableObject.Speed * Time.deltaTime);

        public void SetOrderInLayer(int orderInLayer) => bloonView.SetSortingOrder(orderInLayer);

        private void PopBloon()
        {
            SetState(BloonState.POPPED);
            bloonView.PopBloon();
        }

        public void OnPopAnimationPlayed()
        {
            if (HasLayeredBloons())
                SpawnLayeredBloons();

            playerService.GetReward(bloonScriptableObject.Reward);
            waveService.RemoveBloon(this);
        }

        private bool HasLayeredBloons() => bloonScriptableObject.LayeredBloons.Count > 0;

        private void SpawnLayeredBloons() => CoroutineRunner.Instance.RunCoroutine(waveService.SpawnBloons(bloonScriptableObject.LayeredBloons,
                                                                                          bloonView.transform.position,
                                                                                          currentWaypointIndex,
                                                                                          bloonScriptableObject.LayerBloonSpawnRate));

        public BloonType GetBloonType() => bloonScriptableObject.Type;

        private void SetState(BloonState state) => currentState = state;
    }

    public enum BloonState
    {
        ACTIVE,
        POPPED
    }
}
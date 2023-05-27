using UnityEngine;

[RequireComponent(typeof(AudioSource))]
/// <summary>
/// Simple yet good way to handle player footsteps, jumping, and other sound effects.
/// </summary>
public class PlayerAudio : MonoBehaviour {
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] public AudioSource source;

    /**
    Material-based footstep sound effects. They're arrays because you're supposed to have more variants for each material
    to have that natural randomness.
    */
    [field : SerializeField] public AudioWrapper[] footsteps; // For footsteps that vary depending on materials
    [field : SerializeField] public AudioWrapper[] landSfx; // For when controller lands from a fall that also varies depending on materials
    [SerializeField] public AudioClip[] defaultFootsteps; // Default footsteps if your ground is still untagged
    [SerializeField] public AudioClip[] defaultLandSfx; // Default landSfx if your ground is still untagged
    [SerializeField] public AudioClip jumpSound;
    [SerializeField] public AudioClip slideSound;
    [SerializeField] public float footstepRate; // How frequent the footsteps are produced
    private void Update()
    {
        Footsteps();
    }

    float Distance;
    RaycastHit footHit;
    public int currentMaterial;
    public bool doFootsteps, doLandSfx;
    private void Footsteps()
	{
        // Don't make footsteps when crouching, standard for multiplayer and stealth games.
		if (playerManager.playerMovement.isCrouching)
		{
			return;
		}
		if (playerManager.playerMovement.coyoteGrounded()) // Do footsteps only when grounded.
		{
			float speed = new Vector3(playerManager.playerMovement.velocity.x, 0, playerManager.playerMovement.velocity.z).magnitude;
			if (speed > 20f)
			{
				speed = 20f;
			}
            if (playerManager.playerMovement.isMoving) // Reset when not moving, good for multiplayer and tactical shooters.
            {
			    Distance += speed * footstepRate * Time.deltaTime * 50f;
            } else
            {
                Distance = 0;
            }
			if (Distance > 100f / 1f)
			{
                doFootsteps = true;
                DoSFX(doFootsteps);
			}
		}
	}

    AudioClip[] currentLandSfx;
    AudioClip[] currentFootsteps;
    public void DoSFX(bool action)
	{
        // Makes a raycast at the bottom of the controller to check for the tag of the gameobject it is stepping on
        if (Physics.Raycast(transform.position, Vector3.down, out footHit, (playerManager.controller.height / 2) +
        (playerManager.playerMovement.moveData.groundDistance), playerManager.playerMovement.moveData.groundMask))
        {
            // Make sure the indexes of the AudioClip arrays match the indexes of the strings at MaterialManager.
            // (eg materialTags[0] = Material/Wood, then footsteps[0].audioClip should have wooden footstep sound effects.
            currentMaterial = System.Array.IndexOf(MaterialManager.Instance.materialTags, footHit.transform.gameObject.tag); // Array.IndexOf lets us find the specific index of a string in the array
            // Checks if currentMaterial >= 0 because it becomes -1 which causes it to go out of bounds from the array.
            currentFootsteps = currentMaterial >= 0 ? footsteps[currentMaterial].audioClip : defaultFootsteps;
            currentLandSfx = currentMaterial >= 0 ? landSfx[currentMaterial].audioClip : defaultLandSfx;
        }
        if (action = doFootsteps)
        {
            source.clip = currentFootsteps[Random.Range(0, currentFootsteps.Length)];
            source.pitch = Random.Range(0.8f, 1); // Randomize pitch to make it more realistic.
            
			Distance = 0f; // Reset distance so it calculates again.
        } else if (action = doLandSfx)
        {
            source.clip = currentLandSfx[Random.Range(0, currentLandSfx.Length)];
            source.pitch = 1;
        }
        source.PlayOneShot(source.clip); // PlayOneShot not Play otherwise it'll interrupt the current sound that is playing.
		Distance = 0f; // Reset distance so it calculates again.
        doFootsteps = false;
        doLandSfx = false;
	}
}

[System.Serializable]
public struct AudioWrapper
{
    public AudioClip[] audioClip;
}
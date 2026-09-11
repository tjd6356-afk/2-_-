using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private float speed = 30f;
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody rb;

    private float damage;

    public float Damage => damage;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }


    private void Start()
    {
        // 일정 시간이 지나면 자동 삭제
        Destroy(gameObject, lifeTime);
    }


    public void Launch(
    Vector3 direction,
    float projectileDamage
)
    {
        direction.Normalize();

        damage = projectileDamage;

        rb.linearVelocity =
            direction * speed;
    }


    private void OnCollisionEnter(Collision collision)
    {
        // 현재는 무엇인가에 부딪히면 총알 삭제
        Destroy(gameObject);
    }
}
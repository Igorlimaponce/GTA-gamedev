# GTA 3D — Open World Racing

> Trabalho acadêmico — Disciplina de Desenvolvimento de Jogos · Unity 6

## Descrição

**GTA 3D** é um jogo de mundo aberto em terceira pessoa desenvolvido em Unity 6 com pipeline Built-in. O jogador explora uma cidade low-poly, caminha pelas ruas e pode entrar em um carro para dirigir livremente pelo cenário. A câmera orbital segue o personagem ou o veículo, com controle suave pelo mouse.

---

## Controles

| Ação | Tecla |
|------|-------|
| Mover | WASD |
| Câmera | Mouse |
| Correr | Shift |
| Pular | Espaço |
| Entrar / Sair do carro | E |
| Pausar | Esc |
| Screenshot | F12 |

---

## Gameplay (Vídeo)

<!-- Cole aqui o link do YouTube ou embed após gravar o vídeo -->
> 🎬 **[Clique aqui para assistir ao gameplay no YouTube](SEU_LINK_AQUI)**

---

## Screenshots

> **1 print do Menu Principal**

![Menu Principal](docs/screenshot_menu.png)

> **2 prints do Jogo**

![Gameplay 1 — Personagem na cidade](docs/screenshot_gameplay1.png)

![Gameplay 2 — Dirigindo o carro](docs/screenshot_gameplay2.png)

---

## Funcionalidades Desenvolvidas

### 1. Movimento em 3ª Pessoa (`ThirdPersonPlayerController.cs`)

O sistema de movimento usa um **Rigidbody** para física realista, com a direção calculada **em relação à câmera** — então "pra frente" é sempre para onde a câmera aponta, não o eixo global Z. Corrida com Shift e pulo com verificação de chão via `SphereCast`.

```csharp
// Direção relativa à câmera, projetada no plano horizontal.
Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
Vector3 camRight   = Vector3.Scale(cameraTransform.right,   new Vector3(1, 0, 1)).normalized;

Vector3 moveDir = (camForward * v + camRight * h);
if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

bool running = Input.GetKey(KeyCode.LeftShift);
float targetSpeed = running ? runSpeed : walkSpeed;

// Aplica velocidade preservando a componente vertical (gravidade/pulo).
_rb.linearVelocity = new Vector3(
    moveDir.x * targetSpeed,
    _rb.linearVelocity.y,
    moveDir.z * targetSpeed
);

// Rotaciona suavemente o personagem para a direção do movimento.
if (moveDir.sqrMagnitude > 0.001f)
{
    Quaternion target = Quaternion.LookRotation(moveDir, Vector3.up);
    _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, target, rotationSpeed * Time.fixedDeltaTime));
}
```

> 📸 *(insira aqui um print do personagem andando)*

---

### 2. Entrar e Dirigir o Carro (`VehicleEnterExit.cs` + `CarController.cs`)

Ao pressionar **E** perto do carro, o script detecta a distância, desativa o controle do personagem, parenteia o modelo dentro do veículo e transfere o controle da câmera para seguir o carro. Ao sair, o processo é invertido e o player é reposicionado ao lado do veículo.

```csharp
private void EnterCar()
{
    // Desativa controle do player e oculta o modelo.
    _player.SetControlEnabled(false);
    _playerTransform.SetParent(transform);
    _playerTransform.localPosition = new Vector3(0f, 0.2f, 0f);
    _playerTransform.gameObject.SetActive(false);

    // Ativa o CarController do carro.
    _car.SetControlEnabled(true);

    // Câmera passa a seguir o carro.
    if (_cam != null)
        _cam.SetTarget(transform, new Vector3(0f, 1.2f, 0f));

    _playerInCar = true;
    GameManager.Instance?.SetHint("[E] Sair do carro");
}
```

O `CarController` usa força via `Rigidbody.AddForce` para aceleração e rotação progressiva:

```csharp
// Aceleração na direção frontal do carro.
if (Mathf.Abs(currentSpeed) < maxSpeed)
    _rb.AddForce(transform.forward * accelInput * acceleration, ForceMode.Acceleration);

// Esterçamento proporcional à velocidade (sem WheelColliders — mais estável).
if (Mathf.Abs(currentSpeed) > 0.5f)
{
    float sign = Mathf.Sign(currentSpeed);
    float steerAngle = steerInput * steerSpeed * sign * Time.fixedDeltaTime;
    _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, steerAngle, 0f));
}
```

> 📸 *(insira aqui um print do personagem dentro do carro)*

---

## Como Rodar

1. Abra o projeto no **Unity 6** (`6000.4.11f1` ou superior).
2. No menu do Unity: **GTA → Setup Completo** (executa uma vez).
3. Abra a cena `Assets/_Projeto_GTA/Scenes/MainMenu.unity`.
4. Pressione **Play**.

> **Requisito:** o repositório não inclui a pasta `Library/` (gerada automaticamente). Na primeira abertura, o Unity importará os assets — aguarde a compilação.

---

## Assets Utilizados

| Asset | Fonte | Licença |
|-------|-------|---------|
| Cartoon City Free (ithappy) | Unity Asset Store | Free |
| Drivable-Free Low Poly Cars | Unity Asset Store | Free |
| POLYGON - Starter Pack (Synty) | Unity Asset Store | Free |
| Trilha sonora | Gerada via síntese (Python) | Royalty-free |

---

## Autor

**Igor Lima Ponce** · hiomalima@gmail.com

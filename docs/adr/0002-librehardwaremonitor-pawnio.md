# 0002 — LibreHardwareMonitor + PawnIO para sensores

**Contexto.** Ler tensões e temperaturas da placa no Windows exige um driver de kernel e conhecimento específico de cada chip (registradores, fórmulas, multiplicadores). Escrever isso do zero levaria semanas de engenharia reversa por placa.

**Decisão.** Usar a LibreHardwareMonitorLib (MPL-2.0, versão 0.9.6), que traz suporte a centenas de placas e usa o driver assinado PawnIO. Só o projeto `PcMonitor.Infrastructure.Sensors.Lhm` referencia a biblioteca (garantido por teste de arquitetura).

**Alternativas.** Rust ou C++ com implementação própria (sem biblioteca madura equivalente); WMI (não expõe os rails de tensão).

**Consequências.** A leitura exige administrador, por isso roda no serviço coletor. O suporte ao chip da placa precisa ser validado (spike do Plano 01). A biblioteca é usada sem modificação, então o projeto pode ser MIT.

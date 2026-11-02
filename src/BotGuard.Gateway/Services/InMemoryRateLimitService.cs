using System.Collections.Concurrent;

namespace BotGuard.Gateway.Services;

/// <summary>
/// Сервис ограничения частоты запросов (Rate Limiting) в оперативной памяти.
/// 
/// <para>
/// Реализует алгоритм <b>Sliding Window (Скользящее окно)</b>.
/// Позволяет отслеживать количество запросов от каждого IP-адреса за определенный
/// интервал времени (по умолчанию 10 секунд) и блокировать тех, кто превысил лимит.
/// </para>
/// 
/// <para>
/// <b>Почему ConcurrentDictionary?</b> Этот сервис вызывается одновременно из десятков
/// параллельных HTTP-запросов. Обычный Dictionary при одновременной записи
/// может повредить внутреннюю структуру (race condition) и вызвать
/// InvalidOperationException или зацикливание.
/// </para>
/// 
/// <para>
/// <b>Почему lock (queue)?</b> Мы блокируем не весь словарь, а только очередь
/// конкретного IP. Это минимизирует конкуренцию (Contention) и позволяет
/// тысячам разных IP проходить проверку параллельно.
/// </para>
/// 
/// <para>
/// <b>Почему Environment.TickCount64?</b> Мы не используем DateTime.UtcNow, так как
/// системное время может быть изменено (NTP-синхронизация), что сломает алгоритм
/// окна. TickCount64 — это монотонный счетчик, который всегда растет.
/// </para>
/// </summary>
public class InMemoryRateLimitService
{
    private const int MaxRequests = 5;
    private const long WindowSizeMs = 10_000;
    private readonly ConcurrentDictionary<string, Queue<long>> _clients = new();

    public bool TryAcquire(string ipAddress)
    {
        var queue = _clients.GetOrAdd(ipAddress, static _ => new Queue<long>());

        var msNow = Environment.TickCount64;
        var unused = msNow - WindowSizeMs;
        
        lock (queue)
        {
            while (queue.Count > 0 && queue.Peek() < unused)
                queue.Dequeue();
            
            if(queue.Count >= MaxRequests)
                return false;
            
            queue.Enqueue(msNow);
            return true;    
        }
    }
}
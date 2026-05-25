using System.Numerics;
long CombLong(int n, int k) {
    if (k < 0 || k > n) return 0;
    if (k == 0 || k == n) return 1;
    if (k > n / 2) k = n - k;
    long result = 1;
    for (int i = 1; i <= k; i++) result = result * (n - k + i) / i;
    return result;
}
int pula = 9;
long total = CombLong(26, 13);
decimal sum = 0;
for (int i = 0; i < (1 << pula); i++) {
    int wCount = BitOperations.PopCount((uint)i);
    sum += (decimal)CombLong(26 - pula, 13 - wCount) / total;
}
Console.WriteLine($"C(26,13)={total}");
Console.WriteLine($"Liczba ukladow: {1<<pula}");
Console.WriteLine($"Suma wag: {sum}");
Console.WriteLine($"Roznica od 1: {sum - 1m}");

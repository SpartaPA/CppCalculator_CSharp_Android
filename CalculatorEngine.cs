using System.Globalization;

namespace CppCalculator;

public enum ValueType { Number, Set, Matrix, Sequence }

public sealed class Value
{
    public ValueType Type { get; }
    public double Number { get; }
    public List<double> Sequence { get; }
    public List<double> Set { get; }
    public double[,] Matrix { get; }

    private Value(ValueType type, double number = 0,
        List<double>? sequence = null, List<double>? set = null, double[,]? matrix = null)
    {
        Type = type; Number = number; Sequence = sequence ?? new();
        Set = set ?? new(); Matrix = matrix ?? new double[0, 0];
    }

    public static Value Num(double v) => new(ValueType.Number, number: v);
    public static Value Seq(List<double> v) => new(ValueType.Sequence, sequence: v);
    public static Value SetV(List<double> v) => new(ValueType.Set, set: v);
    public static Value Mat(double[,] v) => new(ValueType.Matrix, matrix: v);
}

public sealed class CalculatorEngine
{
    private enum TokenType { Num, Pow, Mul, Plus, Minus, Div, LParen, RParen, LBrace, RBrace, LBrack, RBrack, Comma, Eof }
    private record Token(TokenType Type, double Value = 0);

    private List<Token> tokens = new();
    private int tp;

    public Value Calc(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) throw new Exception("입력이 비어 있습니다");
        tokens = Tokenize(input);
        tp = 0;
        var n = ParseExpression();
        Expect(TokenType.Eof);
        return Evaluate(n);
    }

    private List<Token> Tokenize(string x)
    {
        var t = new List<Token>();
        int i = 0;
        while (i < x.Length)
        {
            char c = x[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (char.IsDigit(c) || c == '.')
            {
                int j = i;
                while (j < x.Length && (char.IsDigit(x[j]) || x[j] == '.')) j++;
                string raw = x[i..j];
                if (raw.Count(ch => ch == '.') > 1 || !raw.Any(char.IsDigit))
                    throw new Exception("숫자 형식이 올바르지 않습니다: " + raw);
                t.Add(new(TokenType.Num, double.Parse(raw, CultureInfo.InvariantCulture)));
                i = j; continue;
            }
            if (c == '*')
            {
                if (i + 1 < x.Length && x[i + 1] == '*') { t.Add(new(TokenType.Pow)); i += 2; }
                else { t.Add(new(TokenType.Mul)); i++; }
                continue;
            }
            TokenType? tt = c switch
            {
                '+' => TokenType.Plus, '-' => TokenType.Minus, '/' => TokenType.Div,
                '(' => TokenType.LParen, ')' => TokenType.RParen,
                '{' => TokenType.LBrace, '}' => TokenType.RBrace,
                '[' => TokenType.LBrack, ']' => TokenType.RBrack,
                ',' => TokenType.Comma, _ => null
            };
            if (tt is null) throw new Exception("알 수 없는 문자입니다: " + c);
            t.Add(new(tt.Value)); i++;
        }
        t.Add(new(TokenType.Eof));
        return t;
    }

    private abstract record Node;
    private record NumNode(double V) : Node;
    private record GroupNode(Node E) : Node;
    private record NegNode(Node E) : Node;
    private record SeqNode(List<Node> E) : Node;
    private record SetNode(List<Node> E) : Node;
    private record MatNode(List<List<Node>> R) : Node;
    private record BinNode(string Op, Node L, Node R) : Node;

    private Token Peek() => tokens[tp];
    private Token Next() => tokens[tp++];
    private void Expect(TokenType type)
    {
        var z = Next();
        if (z.Type != type) throw new Exception($"'{type}' 가 필요한데 '{z.Type}' 를 찾았습니다");
    }

    private Node ParseExpression()
    {
        var n = ParseTerm();
        while (Peek().Type is TokenType.Plus or TokenType.Minus)
            n = new BinNode(Next().Type == TokenType.Plus ? "+" : "-", n, ParseTerm());
        return n;
    }
    private Node ParseTerm()
    {
        var n = ParseUnary();
        while (Peek().Type is TokenType.Mul or TokenType.Div)
            n = new BinNode(Next().Type == TokenType.Mul ? "*" : "/", n, ParseUnary());
        return n;
    }
    private Node ParseUnary()
    {
        if (Peek().Type == TokenType.Minus) { Next(); return new NegNode(ParseUnary()); }
        return ParsePower();
    }
    private Node ParsePower()
    {
        var b = ParsePrimary();
        if (Peek().Type == TokenType.Pow) { Next(); return new BinNode("**", b, ParseUnary()); }
        return b;
    }
    private Node ParsePrimary()
    {
        var t = Peek();
        if (t.Type == TokenType.Num) { Next(); return new NumNode(t.Value); }
        if (t.Type == TokenType.LParen)
        {
            Next();
            var e = new List<Node> { ParseExpression() };
            while (Peek().Type == TokenType.Comma) { Next(); e.Add(ParseExpression()); }
            Expect(TokenType.RParen);
            return e.Count == 1 ? new GroupNode(e[0]) : new SeqNode(e);
        }
        if (t.Type == TokenType.LBrace)
        {
            Next();
            var e = new List<Node> { ParseExpression() };
            while (Peek().Type == TokenType.Comma) { Next(); e.Add(ParseExpression()); }
            Expect(TokenType.RBrace); return new SetNode(e);
        }
        if (t.Type == TokenType.LBrack)
        {
            Next();
            var rows = new List<List<Node>>();
            if (Peek().Type == TokenType.LBrack)
            {
                rows.Add(ParseRow());
                while (Peek().Type == TokenType.Comma) { Next(); rows.Add(ParseRow()); }
            }
            else
            {
                var row = new List<Node> { ParseExpression() };
                while (Peek().Type == TokenType.Comma) { Next(); row.Add(ParseExpression()); }
                rows.Add(row);
            }
            Expect(TokenType.RBrack); return new MatNode(rows);
        }
        throw new Exception("예상치 못한 토큰입니다: " + t.Type);
    }
    private List<Node> ParseRow()
    {
        Expect(TokenType.LBrack);
        var row = new List<Node> { ParseExpression() };
        while (Peek().Type == TokenType.Comma) { Next(); row.Add(ParseExpression()); }
        Expect(TokenType.RBrack); return row;
    }

    private Value Evaluate(Node n) => n switch
    {
        NumNode x => Value.Num(x.V),
        GroupNode x => Evaluate(x.E),
        NegNode x => Neg(Evaluate(x.E)),
        SeqNode x => Value.Seq(x.E.Select(e => NumberOnly(Evaluate(e))).ToList()),
        SetNode x => Value.SetV(Dedupe(x.E.Select(e => NumberOnly(Evaluate(e))).ToList())),
        MatNode x => MakeMatrix(x.R),
        BinNode x => Apply(x.Op, Evaluate(x.L), Evaluate(x.R)),
        _ => throw new Exception("알 수 없는 노드입니다")
    };

    private static double NumberOnly(Value v) =>
        v.Type == ValueType.Number ? v.Number : throw new Exception("원소는 숫자여야 합니다");

    private static Value MakeMatrix(List<List<Node>> rows)
    {
        int r = rows.Count, c = rows[0].Count;
        if (rows.Any(x => x.Count != c)) throw new Exception("행렬의 각 행 길이가 동일해야 합니다");
        var m = new double[r, c];
        for (int i = 0; i < r; i++) for (int j = 0; j < c; j++) m[i,j] = NumberOnly(new CalculatorEngine().Evaluate(rows[i][j]));
        return Value.Mat(m);
    }

    private static Value Neg(Value v) => v.Type switch
    {
        ValueType.Number => Value.Num(-v.Number),
        ValueType.Sequence => Value.Seq(v.Sequence.Select(x => -x).ToList()),
        ValueType.Set => Value.SetV(Dedupe(v.Set.Select(x => -x).ToList())),
        ValueType.Matrix => Value.Mat(Scale(v.Matrix, -1)),
        _ => throw new Exception("음수로 만들 수 없는 타입입니다")
    };

    private static Value Apply(string op, Value a, Value b) => op switch
    {
        "+" => Add(a,b), "-" => Sub(a,b), "*" => Mul(a,b), "/" => Div(a,b), "**" => Pow(a,b),
        _ => throw new Exception("알 수 없는 연산자입니다: " + op)
    };

    private static Value Add(Value a, Value b)
    {
        if (a.Type == ValueType.Number && b.Type == ValueType.Number) return Value.Num(a.Number+b.Number);
        if (a.Type == ValueType.Matrix && b.Type == ValueType.Matrix) return Value.Mat(MatAdd(a.Matrix,b.Matrix));
        if (a.Type == ValueType.Sequence && b.Type == ValueType.Sequence) return Value.Seq(Zip(a.Sequence,b.Sequence,(x,y)=>x+y,"덧셈"));
        if (a.Type == ValueType.Set && b.Type == ValueType.Set) return Value.SetV(Dedupe(a.Set.Concat(b.Set).ToList()));
        throw new Exception($"덧셈은 같은 타입끼리만 가능합니다 ({Name(a)} + {Name(b)})");
    }
    private static Value Sub(Value a, Value b)
    {
        if (a.Type == ValueType.Number && b.Type == ValueType.Number) return Value.Num(a.Number-b.Number);
        if (a.Type == ValueType.Matrix && b.Type == ValueType.Matrix) return Value.Mat(MatSub(a.Matrix,b.Matrix));
        if (a.Type == ValueType.Sequence && b.Type == ValueType.Sequence) return Value.Seq(Zip(a.Sequence,b.Sequence,(x,y)=>x-y,"뺄셈"));
        if (a.Type == ValueType.Set && b.Type == ValueType.Set) return Value.SetV(a.Set.Where(x=>!b.Set.Any(y=>Key(y)==Key(x))).ToList());
        throw new Exception($"뺄셈은 같은 타입끼리만 가능합니다 ({Name(a)} - {Name(b)})");
    }
    private static Value Mul(Value a, Value b)
    {
        if (a.Type == ValueType.Number && b.Type == ValueType.Number) return Value.Num(a.Number*b.Number);
        if (a.Type == ValueType.Number && b.Type == ValueType.Matrix) return Value.Mat(Scale(b.Matrix,a.Number));
        if (b.Type == ValueType.Number && a.Type == ValueType.Matrix) return Value.Mat(Scale(a.Matrix,b.Number));
        if (a.Type == ValueType.Number && b.Type == ValueType.Set) return Value.SetV(Dedupe(b.Set.Select(x=>x*a.Number).ToList()));
        if (b.Type == ValueType.Number && a.Type == ValueType.Set) return Value.SetV(Dedupe(a.Set.Select(x=>x*b.Number).ToList()));
        if (a.Type == ValueType.Number && b.Type == ValueType.Sequence) return Value.Seq(b.Sequence.Select(x=>x*a.Number).ToList());
        if (b.Type == ValueType.Number && a.Type == ValueType.Sequence) return Value.Seq(a.Sequence.Select(x=>x*b.Number).ToList());
        if (a.Type == ValueType.Matrix && b.Type == ValueType.Matrix) return Value.Mat(MatMul(a.Matrix,b.Matrix));
        if (a.Type == ValueType.Sequence && b.Type == ValueType.Sequence) return Value.Seq(Zip(a.Sequence,b.Sequence,(x,y)=>x*y,"곱셈"));
        if (a.Type == ValueType.Set && b.Type == ValueType.Set) return Value.SetV(a.Set.Where(x=>b.Set.Any(y=>Key(y)==Key(x))).ToList());
        throw new Exception($"곱할 수 없는 타입 조합입니다 ({Name(a)} * {Name(b)})");
    }
    private static Value Div(Value a, Value b)
    {
        if (a.Type == ValueType.Number && b.Type == ValueType.Number) { if(b.Number==0) throw new Exception("0으로 나눌 수 없습니다"); return Value.Num(a.Number/b.Number); }
        if (a.Type == ValueType.Matrix && b.Type == ValueType.Matrix) return Value.Mat(MatMul(a.Matrix,Inverse(b.Matrix)));
        if (a.Type == ValueType.Matrix && b.Type == ValueType.Number) { if(b.Number==0) throw new Exception("0으로 나눌 수 없습니다"); return Value.Mat(Scale(a.Matrix,1/b.Number)); }
        if (a.Type == ValueType.Sequence && b.Type == ValueType.Number) { if(b.Number==0) throw new Exception("0으로 나눌 수 없습니다"); return Value.Seq(a.Sequence.Select(x=>x/b.Number).ToList()); }
        if (a.Type == ValueType.Sequence && b.Type == ValueType.Sequence) return Value.Seq(Zip(a.Sequence,b.Sequence,(x,y)=>{if(y==0)throw new Exception("0으로 나눌 수 없습니다");return x/y;},"나눗셈"));
        throw new Exception($"나눌 수 없는 타입 조합입니다 ({Name(a)} / {Name(b)})");
    }
    private static Value Pow(Value a, Value b)
    {
        if (b.Type != ValueType.Number) throw new Exception("지수는 숫자여야 합니다");
        if (a.Type == ValueType.Number) return Value.Num(Math.Pow(a.Number,b.Number));
        if (a.Type == ValueType.Matrix) return Value.Mat(MatPow(a.Matrix,b.Number));
        if (a.Type == ValueType.Sequence) return Value.Seq(a.Sequence.Select(x=>Math.Pow(x,b.Number)).ToList());
        throw new Exception($"제곱을 지원하지 않는 타입입니다 ({Name(a)})");
    }

    private static string Name(Value v) => v.Type switch { ValueType.Number=>"실수",ValueType.Set=>"집합",ValueType.Matrix=>"행렬",ValueType.Sequence=>"수열",_=>"" };
    private static double Key(double x)=>Math.Round(x,9);
    private static List<double> Dedupe(List<double> a)=>a.GroupBy(Key).Select(g=>g.First()).ToList();
    private static List<double> Zip(List<double>a,List<double>b,Func<double,double,double> f,string name)
    { if(a.Count!=b.Count) throw new Exception($"수열의 길이가 서로 다릅니다 ({name} 불가): {a.Count} vs {b.Count}"); return a.Select((x,i)=>f(x,b[i])).ToList(); }

    private static double[,] MatAdd(double[,]a,double[,]b)=>MatMap2(a,b,(x,y)=>x+y,"덧셈");
    private static double[,] MatSub(double[,]a,double[,]b)=>MatMap2(a,b,(x,y)=>x-y,"뺄셈");
    private static double[,] MatMap2(double[,]a,double[,]b,Func<double,double,double>f,string op)
    { if(a.GetLength(0)!=b.GetLength(0)||a.GetLength(1)!=b.GetLength(1)) throw new Exception($"행렬 크기가 맞지 않습니다 ({op} 불가): {a.GetLength(0)}x{a.GetLength(1)} vs {b.GetLength(0)}x{b.GetLength(1)}"); var m=new double[a.GetLength(0),a.GetLength(1)]; for(int i=0;i<m.GetLength(0);i++)for(int j=0;j<m.GetLength(1);j++)m[i,j]=f(a[i,j],b[i,j]);return m;}
    private static double[,] MatMul(double[,]a,double[,]b)
    { int ar=a.GetLength(0),ac=a.GetLength(1),br=b.GetLength(0),bc=b.GetLength(1); if(ac!=br)throw new Exception($"행렬 크기가 맞지 않습니다 (곱셈 불가): {ar}x{ac} * {br}x{bc}"); var m=new double[ar,bc]; for(int i=0;i<ar;i++)for(int j=0;j<bc;j++)for(int k=0;k<ac;k++)m[i,j]+=a[i,k]*b[k,j];return m;}
    private static double[,] Scale(double[,]a,double s){var m=(double[,])a.Clone();for(int i=0;i<m.GetLength(0);i++)for(int j=0;j<m.GetLength(1);j++)m[i,j]*=s;return m;}
    private static double[,] Identity(int n){var m=new double[n,n];for(int i=0;i<n;i++)m[i,i]=1;return m;}
    private static double[,] Inverse(double[,]m)
    { int n=m.GetLength(0);if(n!=m.GetLength(1))throw new Exception("정사각행렬만 역행렬을 구할 수 있습니다");var a=(double[,])m.Clone();var inv=Identity(n);for(int c=0;c<n;c++){int p=c;for(int r=c;r<n;r++)if(Math.Abs(a[r,c])>Math.Abs(a[p,c]))p=r;if(Math.Abs(a[p,c])<1e-12)throw new Exception("역행렬이 존재하지 않습니다 (특이행렬)");for(int j=0;j<n;j++){(a[c,j],a[p,j])=(a[p,j],a[c,j]);(inv[c,j],inv[p,j])=(inv[p,j],inv[c,j]);}double d=a[c,c];for(int j=0;j<n;j++){a[c,j]/=d;inv[c,j]/=d;}for(int r=0;r<n;r++){if(r==c)continue;double f=a[r,c];for(int j=0;j<n;j++){a[r,j]-=f*a[c,j];inv[r,j]-=f*inv[c,j];}}}return inv;}
    private static double[,] MatPow(double[,]m,double power)
    { if(power!=Math.Truncate(power))throw new Exception("행렬의 지수는 정수여야 합니다");int n=m.GetLength(0);if(n!=m.GetLength(1))throw new Exception("정사각행렬만 거듭제곱할 수 있습니다");int e=(int)power;if(e<0)return MatPow(Inverse(m),-e);var r=Identity(n);var b=m;while(e>0){if((e&1)==1)r=MatMul(r,b);b=MatMul(b,b);e>>=1;}return r;}

    public static string Format(Value v)
    {
        string F(double x) { if (Math.Abs(x) < 1e-12) x=0; return Math.Round(x,9).ToString("G",CultureInfo.InvariantCulture); }
        return v.Type switch
        {
            ValueType.Number=>F(v.Number),
            ValueType.Set=>"{" + string.Join(", ",v.Set.Select(F)) + "}",
            ValueType.Sequence=>"(" + string.Join(", ",v.Sequence.Select(F)) + ")",
            ValueType.Matrix=>FormatMatrix(v.Matrix,F),
            _=>v.ToString()!
        };
    }
    private static string FormatMatrix(double[,]m,Func<double,string>F)
    { int r=m.GetLength(0),c=m.GetLength(1); if(r==1)return "["+string.Join(", ",Enumerable.Range(0,c).Select(j=>F(m[0,j])))+"]"; return "["+string.Join(", ",Enumerable.Range(0,r).Select(i=>"["+string.Join(", ",Enumerable.Range(0,c).Select(j=>F(m[i,j])))+"]"))+"]"; }
}
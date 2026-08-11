using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;

namespace CppCalculator;

public class CalculatorPage : ContentPage
{
    readonly CalculatorEngine engine = new();
    readonly Label expr = new();
    readonly Label result = new();
    string text = "";
    int cursor = 0;

    readonly Color ink = Color.FromArgb("#3B3350");
    readonly Color soft = Color.FromArgb("#8A81A0");
    readonly Color seq = Color.FromArgb("#CFE7FF");
    readonly Color set = Color.FromArgb("#FFD3E8");
    readonly Color mat = Color.FromArgb("#D3F5D0");
    readonly Color num = Color.FromArgb("#FFF6DD");
    readonly Color op = Color.FromArgb("#FFDCC7");
    readonly Color eq = Color.FromArgb("#D9C9FF");
    readonly Color err = Color.FromArgb("#E5537A");

    public CalculatorPage()
    {
        Title = "Cpp Calculator";
        BackgroundColor = Color.FromArgb("#F3E9FF");
        Padding = new Thickness(16,32,16,40);

        var title = new Label { Text="Cpp Calculator", FontSize=30, FontAttributes=FontAttributes.Bold, HorizontalTextAlignment=TextAlignment.Center, TextColor=ink };
        var desc = new Label { Text="수열 ( , ) · 집합 { , } · 행렬 [ , ] 을 버튼으로만 계산합니다\n같은 타입끼리 연산하면 결과도 같은 타입으로 나옵니다", FontSize=13, HorizontalTextAlignment=TextAlignment.Center, TextColor=soft };

        var legend = new HorizontalStackLayout { HorizontalOptions=LayoutOptions.Center, Spacing=10, Margin=new Thickness(0,12,0,18) };
        legend.Add(Pill("( ) 수열·묶음",seq)); legend.Add(Pill("{ } 집합",set)); legend.Add(Pill("[ ] 행렬",mat));

        var display = new Border { BackgroundColor=Color.FromArgb("#F6FFFA"), Stroke=Color.FromArgb("#E4E0F5"), StrokeThickness=1, StrokeShape=new RoundRectangle{CornerRadius=16}, Padding=16, Margin=new Thickness(0,0,0,14) };
        expr.FontSize=22; expr.FontFamily="monospace"; expr.TextColor=ink; expr.MinimumHeightRequest=32;
        result.FontSize=19; result.FontAttributes=FontAttributes.Bold; result.TextColor=Color.FromArgb("#4B2E99"); result.HorizontalTextAlignment=TextAlignment.End;
        display.Content=new VerticalStackLayout{Spacing=8,Children={
            new Label{Text="CALCULATION AREA",FontSize=10.5,TextColor=soft,FontAttributes=FontAttributes.Bold},
            expr,
            new BoxView{HeightRequest=1,Color=Color.FromArgb("#DCD6EE")},
            new HorizontalStackLayout{HorizontalOptions=LayoutOptions.End,Spacing=8,Children={new Label{Text="RESULT OUTPUT",FontSize=10.5,TextColor=soft},result}}
        }};

        var clear = new Button{Text="전체 지우기 (C)",BackgroundColor=Colors.Transparent,TextColor=soft,FontSize=12,HorizontalOptions=LayoutOptions.End};
        clear.Clicked += (_,_)=>Clear();

        var grid=new Grid{ColumnSpacing=9,RowSpacing=9};
        for(int i=0;i<4;i++)grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        // 키패드는 실제 6행이므로 Row도 6개만 생성
        for(int i=0;i<6;i++)grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        string[][] keys={
            new[] {"(","{","[","⌫"},
            new[] {")","}","]","÷"},
            new[] {"7","8","9","×"},
            new[] {"4","5","6","−"},
            new[] {"1","2","3","+"},
            new[] {"0",".",",","="}
        };

        for(int r=0;r<keys.Length;r++)for(int c=0;c<4;c++)
        {
            var k=keys[r][c];
            var b=new Button{Text=k,FontSize=18,FontAttributes=FontAttributes.Bold,TextColor=ink,CornerRadius=14,Padding=0,HeightRequest=58};
            b.BackgroundColor=k switch{"(" or ")"=>seq,"{" or "}"=>set,"[" or "]"=>mat,"⌫"=>Color.FromArgb("#F0E9FB"),"÷" or "×" or "−" or "+"=>op,"="=>eq,_=>num};
            b.Clicked+=(_,_)=>Click(k);
            grid.Add(b,c,r);
        }

        var footer=new Label{Text="( a, b ) 수열 · { a, b } 집합 · [ [a,b],[c,d] ] 행렬 · 원소 1개짜리 ( )는 그냥 묶음 괄호로 처리됩니다 · 별(*) 두 번 = 제곱(**) · 슬래시(/)는 역행렬 곱 · 입력은 버튼으로만 가능합니다",FontSize=11.5,TextColor=soft,HorizontalTextAlignment=TextAlignment.Center,Margin=new Thickness(0,16,0,0)};

        Content=new ScrollView{Content=new VerticalStackLayout{Spacing=0,Children={title,desc,legend,display,clear,grid,footer}}};
        Render();
    }

    View Pill(string s,Color c)=>new Border{BackgroundColor=c,StrokeThickness=0,StrokeShape=new RoundRectangle{CornerRadius=20},Padding=new Thickness(9,4),Content=new Label{Text=s,FontSize=11.5,FontAttributes=FontAttributes.Bold,TextColor=ink}};

    void Click(string k)
    {
        if(k=="⌫"){if(cursor>0){text=text.Remove(cursor-1,1);cursor--;}Render();return;}
        if(k=="="){try{result.Text=CalculatorEngine.Format(engine.Calc(text));result.TextColor=Color.FromArgb("#4B2E99");}catch(Exception e){result.Text="오류: "+e.Message;result.TextColor=err;}return;}
        k=k switch{"÷"=>"/","×"=>"*","−"=>"-",_=>k};
        text=text.Insert(cursor,k);cursor+=k.Length;Render();
    }

    void Clear(){text="";cursor=0;result.Text="0";result.TextColor=Color.FromArgb("#4B2E99");Render();}

    void Render(){expr.Text=(text.Length==0?"여기에 버튼으로 입력하세요":text)+" |";}
}


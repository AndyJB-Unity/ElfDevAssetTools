using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ElfDev
{
	public class GuiScrollView : System.IDisposable
	{
		public GuiScrollView( ref Vector2 scroll )
		{
			scroll = EditorGUILayout.BeginScrollView( scroll );
		}

		public GuiScrollView( ref Vector2 scroll, params GUILayoutOption[] options )
		{
			scroll = EditorGUILayout.BeginScrollView( scroll, options );
		}

		public GuiScrollView( ref Vector2 scroll, GUIStyle skin, params GUILayoutOption[] options )
		{
			scroll = EditorGUILayout.BeginScrollView( scroll, skin, options );
		}

		public void Dispose()
		{
			EditorGUILayout.EndScrollView();
		}
	}

    public abstract class ElfDevEditorWindow<T> : EditorWindow
        where T : EditorWindow
    {
        [SerializeField]
        private static T s_instance;

        private static Texture s_texture = null;
        private static Texture2D bk_texture = null;

        private System.Exception m_lastError;
        private Vector2 m_errorScroll;

        private GUIStyle m_errorStyle;

        public static T Instance
        {
            get { return s_instance; }
        }

        protected ElfDevEditorWindow(string title)
        {
            titleContent.text = title;
        }

		private const string watermarkImageBase64 = 
@"iVBORw0KGgoAAAANSUhEUgAAAIAAAAB+CAYAAADsphmiAAAABGdBTUEAALGPC/xhBQAAACBjSFJN
AAB6JgAAgIQAAPoAAACA6AAAdTAAAOpgAAA6mAAAF3CculE8AAAACXBIWXMAAAsTAAALEwEAmpwY
AAABWWlUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpu
czptZXRhLyIgeDp4bXB0az0iWE1QIENvcmUgNS40LjAiPgogICA8cmRmOlJERiB4bWxuczpyZGY9
Imh0dHA6Ly93d3cudzMub3JnLzE5OTkvMDIvMjItcmRmLXN5bnRheC1ucyMiPgogICAgICA8cmRm
OkRlc2NyaXB0aW9uIHJkZjphYm91dD0iIgogICAgICAgICAgICB4bWxuczp0aWZmPSJodHRwOi8v
bnMuYWRvYmUuY29tL3RpZmYvMS4wLyI+CiAgICAgICAgIDx0aWZmOk9yaWVudGF0aW9uPjE8L3Rp
ZmY6T3JpZW50YXRpb24+CiAgICAgIDwvcmRmOkRlc2NyaXB0aW9uPgogICA8L3JkZjpSREY+Cjwv
eDp4bXBtZXRhPgpMwidZAABAAElEQVR4Ae2dCZhdRbGA7yxJ2HeQQEISNhEBRVARkE1EUEQEkUVB
MYKgILKoLAoCQVwhgqCigqLIKjyVTURRQFA2ZQ9oWAIksgUIgSSz3Pv+v6b7vHNvZpJJMldmnvb3
3XvO6dNbVVdXVVdX92mpDLFQq9VaNt100/Y777yzMzf93HPPXfn2229/x/Tp07fit8GMGTNGvvTS
S0tsvPHG415++eW2ZZddtjJ16tRpzz777AsjR46c0dnZ+fjw4cOfIM3zb3rTm1qXW265GnEt//rX
v2r8Ks8//3ylq6ursuKKK7asv/763eSbOmrUqGeXWWaZR3fdddcnqP/VXLfXTTbZZNj73//+7q98
5SvVcvxQuG8ZCo3MbRTRuePpqCVPOOGEXaZMmbL3M888szWdtAz3OWm/rhBBZYkllqi0t7dXqtVq
paOjowIhROdbQFtbW2XxxRevtLS0VCSixRZbrLLUUktNg4gegBj+NG7cuN+ecsopt5Uqa5cIhiIh
lGAYfLeXXHJJW27Vn//851UOPPDAU7fYYotpo0ePrhGff53Dhg2bQ4fNpmNn06mzl19++Q46rMMr
8XPo0Nl04izezyGfHKQj/Tp5N4eOns3zbMqxDNPkn+m6+OW6aqSprbvuurXttttu0r777ns8XGg0
73MYJqfKD/+9LgIG6OxhOftnP/vZo975znfOoDNzR3QuueSSHXTqXB1EnkgDIURntba25jxxpbNl
1zmuuCc+xzVeu0nfIWHANeZQnnUaV6P+Gtxpznvf+96zyoRQJlzSDcowaKnUEURw5HedeeaZ6117
7bU//9vf/rYJslxEdjKyW2bOnGknBYGMGTOmssIKK8x43ete98Arr7xy35prrjnzvvvue/KFF17o
XnrppVsff/zx7t12220k4mKp5557bue//OUvYxj11e7u7hauLe95z3seJ+3zt956a+tWW221DB29
DPm7VlpppSURC0vNmTOnBX0i9AMbkEInZVeMJ0Q73vrWt3ZuuOGGp/z4xz8+SWIqi62caTBdByUB
pM63bdUvfelL+1933XXn3nZbiNoOZG87Sp4jbxidU4ENz0AW/xq5fBmIv/ljH/vY8/ND8AEHHPDp
n/3sZ2fNnj1bMdCy1lprtX/qU5/a6wtf+MLF5qX+9oceemjx0047rRtFcil0i+UgttdR79oQ15sh
oLejKG4CUdkWs3RBkBXKq6E8DhsxYkRlp512egjxsCdc627et/MLbmHi/4Z5YCDJziDMww8//Bvr
rLNOsGLYfiesVzlcBdk1NPEnPv7xjx9xww03rNRQXCvPItwROcwR6M/7sWPHLsa1Qqfuxci1XAmg
c+2116598Ytf3Md3ZZHjc1/h1FNPHQshHfGud73r/lVXXTWLiw7KtaPVHWpvfvOba0cdddQBqYzW
BFtfRf43vtz5TLfOSIiV3SujlbmB1P322+8U0kZnJqy1b7PNNu3zQ7BpTP+Rj3xkn1w2j0EAxx57
bBAAGrxpWnJbeG71t8cee7Sl/BKTRFaET3ziEx9EhDwIR8rE2pWItQbR1T796U9/KyXO5RZ5/3tT
j4FALFr1GUSLTLX4ovN32WWXR77zne9skrM4WufX6Tmt10wABx988D5pBlFwgAYCKGfr9V6iSJyl
EKOHHnrokXCmPFvoQEG17Z3aGbbffnthMvyXCHrwMNe/I6vyuc997gRkcu78uBJd23PPPa+is4en
XMMXpONTnoIADjvssH1QGi17oQkgl+kVYgjO4v1Pf/rTdT/wgQ9M4jbaDicoYPjwhz+ciaCOg5jv
PzpkuXvyySd/6A1veEN0DLI0Rj7WuNoRRxxxSUZQGdk5rr/XzAHQLfaRNZNvQAgg14+YCAKFONsR
M1cTH50PJ4grymrtM5/5zJdS+iD4nPc/9qpsFfhf/vKXazLHDzmfFKm4h10Xnb+o8+pMPChm+9gZ
VDugBCAcuQ7vITRnFdH5iaC71ltvvRppdvV9Jnzv/5NDyFDY5q0gocYUSqucHVPba6+9/pgRA9IW
mW3mzlHjT2JmwAnA9pYJlWngz1deeeUgAqawMRXceuutZ/3mN79Zw7R5AHj/HxeSElVhDn4UIySQ
BLsMJL373e+eOnny5GVFShmhi4KkTAAqfE79KKsggC9/+cvlWcCiVBN5ywS7//77X05k1Od01vu9
9977j5GwYUaR4v5tl0UeVQvbUhHkws6FF144FgvfKVrTIII2DC1q1xUIYE9G6UuySZQniWLAAhym
rizMurHwUxe5iA/AV82Ee9555+228847h0GIRSc5XufNN9+89dFHH30Q99XXUhS8pgQgjn/1q199
DSvfcGz2nRBBDSKobL755l/HKncTr4edc845jpgBD5hp68rUbFsXMQAPEm7mOqwT7MbS82xMym1M
bVufeOKJCgtbp2DIWk4YF2ZWMwBNrDdoDESB/SkjjYyur3/965vdf//9e5KnG9nvVKoNJHUzZz4v
l1NmpTluUa9OzcpcwOVg4uopYlErIb9t5xclsUYxixXEl3zAZOzA64QAVvjJT35yjHH6OHj9TwnB
g3GiuAqAayy8hFykI5z61eAAL8MBPpKRMVAsks4IJJ999tn7bLTRRiGTqaPzjW98Yw0D04etL6fJ
dS/stdzm448/fo93vOMddr6EFzoOxBCwsn4xc+LEia+zHup+zTjywsK5wPmy1ost/a2Mdjuh2yVb
ft0gpRsEBWK01DGXvviee+5ZPlXSlvMucKUpQ87P7GIbVg6tW6udddbWWGONLVKyegUhRfb3krhb
dCQrmCuwXnGRUz/yl2EMgkfsxFR3/PjxX0/l/0fYBmIU7r777uclpHSCiBgVPvNzjd/nQNK22277
HMrS3jznsEDm35wpXaNzV1lllc1dwyfOero11W622WZBAJlIGvLN9zGN3qID4WD7M917gYwFTCib
AVOKU+wFsbP8/Ay6wFJW8lrpAtbd9JBZHKNkVRD+ChXW6OywnWMHuGPHHXe83zh+YUdnZMYIcc7O
qL1GM2tupCx2QZGVO5d2bC7bp6wgAEfocccdt5ll5zS5nvldbUOezpr2e9/73saUcdPqq69u+Y56
F7PKBP2YXEGux3sJoFNuBDwf474wV3v//y5kRLF6dihGERFgB3fhvFFjxGwrwHT0b3DADOSpG5SR
97a3vU1EfWvSpElLm9Zgmf0lhEyAp59++lj8CAL5FNEJgXV/7WtfW8Pychrv5xUaO/6mm25a3kUs
HEKi7eSViBVrMepf//rX11jFvPSWW25ZnLytmKWnk8ZZTxD5Bz/4wetTfYskgubV5sHwLjTt973v
fXeUgWdN/YFyJ2JCPZDO1j9PZAYRZEVR1y4UqmcwHh1BnsVLQA3rZ+dFG5ii3UyZ4S7GQtOfUznz
nQmkOgpWbxtcBYTdv5jaW0W8dJXFGO2dgfXxo6W2VvBOOtv0iIEgAGYBs+Fwq5umn3CUixv89xko
EPHGZIWrqgQxQmooQScKAazTxZQYARiIRmMtuzbZ7CUEl4ZlpYEwF4l22GGHaTpl3HHHHWEx5J0h
1u3LBNUT3fOf4zE+LceIPcaf977N78rpvbft/NRditGJa9oSwHIES9TTkkJZ00+w3EadRj/60Y9e
cfnll6+SymzLswNMxNsm2BSBneglLhTtazq4w/+/KWFm/0z9jlX+Jflefctb3lJTbgp4lr8ZScZ9
/vOf34OOnuJo5TFkKmyzIARGWo1FpOm4gn3r29/+dqEjmJcQxGAHNnRubyO9iDOteVJHFJ1ugRht
1kWGn7rlllv+K3c80R24jXdnrR4iqGHJfOSYY47ZxTyGDFNuB4agxWn3U7yq6bHsFSeYn5mW8P+P
AAAqEIyfnM59NZAUQIOou4SYUHSAD3YAl0A+SBt+yCGHnIBYiLk08Y42HTJj9Pjsjzl1DUXyWkcS
7HRF4hqD5Q2zY+0QOnIxf96nzpa113W4BVx11VWrIpY+hjXvGlh11EW010463jYEVzIOG8YM6j+W
Nudy2hIsvC5CdDDm4QuIUQwELt7+9rc/TL5sC6jDR5FzKN5kBODdOw4/uVCKZJeOaub6JwtT5hCN
8JG3GA0YcJZnYeVbEFGBcNLrh+cKYhEnO2WW8RJy9ko4w4E4lr4BxBZyu7GOxmfSjmDBaCPqOoRO
uoo9CDOzYkra4F7Wad0+q5e4lA2RnskqX+GjWG57uY7M4lGGD1SU5XI0TmEdfb1pM87K+ZpxXyC3
GYXnMv/4xz9K1dV77733TU8//bR1znFRhKlSBcT+znTMBKosDnlbF0BEFx0S28HwrXNefRTrB6fh
03fwk08+efDdd9+9Ilu8zNOtogghVHD9buG3DHHvg02/j+XYClr643Cbe1DQHoBApuDN+yS6SJdb
wh588ME2TLWj2Va2JmbaDejw9fH6HeU2sVS25avY0ZRa5dVXX21l3aKNzqswg3hxgw02OBe7xhko
t49/97vfDWJGL+lCJMgd5goQQBWcVIDhFjat6GreAhHNob4RjzzyyHpkeCjjbK7MQzEiy3bcrDdF
Iy5YKJrz9AUxgKRRURAtK2pLM+oOhSP8PSmWuewuOnpOEjMFZwB38V7nTX9OzVTU6IT45ffp6jSx
gzUCdxvJooNz+U67AesVkxjBRzCtW4G4HPo7EzF9sHjESrZ9yLVqEyZM2NSXGWfeD/ngCAaIkIlY
9Q7EujcJ9nwrwG4ucAvK7lL6OpaOIWcrZg3nolROzUYYis4EobjJ28JmEW+HOjrLvzkQzWw6PG8L
KzrccjAVO/2cjp3iF4iUHRNMvOoRXwsKQ+5g7A+bo+TeCU4exCfhwCgQXJXLT3FNuTRV0RAIAy13
NHlVFKjBl0OIh3JEf+8t31U0RIcdaWdXkMFLICK2gU3vhojYik0c6yAOKlzriqVZym49dI2vpXX6
Ig0iwZ1G/h5GZNw8bty4q5nB/J6Ocr4fQb0lsfooJMcvwLU32B0o4ssyYzUR4vJ5aIUyBeMQEfNs
IVDrTpCEr/1AQZUWYeq4Am0QgeujzH0Uw8/psO3fIC7uwgz8NPWK4CpEEMhGhDzLSLwPPeFaOMlE
8uyNk2ooZA1tbKdMxdCADJ7EOSSEMm4qZZyVcdnQlsH5mBvsPjzk5OWs7M2E5d98zTXXjLXFsD8N
PoscrEdWmmYQ0SF2TmKvha7QWBHLs/u6vkC8bL5TDd9pXmO60nN0eprLByezTuvJsJbSLtRtxokG
MIxLN0KQM5nBXPHoo4/O00i1UJU1O1Oe5mBl+xysVETL9p0jP/+DH/xgw1R/3WidR5tiI4UjRYSn
su3cGDV95MvvygYd84wwPesOu5TdwiUGrHofSGWZJnYapdGZR3q+pmR1F+uLPLbRfIkw5pWnXEDg
QtygZ7i3MXCGTuJ+CE3GlYzTcqbBfB+jj0afSiPD3q1W7j1abxUqD+UvU30DIC397OQKO3CXpjM3
Y6R8iXn4X2Dvk1AG90jlZUNMUXzq0Mq3vvWtMckhRMLs8v6MM85Yy4Q5TZGp5ybKQknbj/Y/qLEJ
4j4eotnyd7/7XdkM3ZAtHgvi4Gkugsg4ECfihjRhGQRfMXtB6fxZKrRPjpbeD6pLNJbOOFeAmJuH
Rs08Oq5M/6p44AQRNBiA5kIQI6ntggsuWJ7Zw9pM+bb75Cc/+RlMpmexgHQjmzyfcSVRFy/r8eeK
4Ve/+tUwCc+rM1nAORvFLqaC3ifs9Uk06gPlKSzWu5qcA672HPsC/0SbTj/ooIMOYNfRVqQdh06y
rG1P5ZYvBYwZ9rPOOmtziD46P+MI3SRwBXGfljIPPQJgZH2FxtsxQc3eJwuaiKsKOHGFndx7kTZ+
/PjD2F59KZ18K+bRyXgOTWe+3u12rpL9PXd6N8hy6VUO40Z9uczulpU4ibflUHQABLUrW9F2Lb0s
3uW4XAYzAMu0zpetK1keQ7Sl+Jr+/xIVSmYnxPks6wWT+d0KLJdDHIeXCSKvDYgD0kTnZ9yk8jqo
w/qO5WcYegRAo49zusU1CICOCrMpBpqgbrTy2vnnn58NH7G0ywg/Lsnn3MHlq9O9fEqHx8DUGWh4
V8OxZBrTwHn62PXGGZTd5J8r5LSWadkkKLenE/vCHAgijqXhnXDmKWk5XdgRmFkcZwXUFbAKO7OO
SCdOxE/Cl3ESdW2fffY5wTyZEL0f9CE3llFzvIoMDe4QOK7+NNlW8wYJ0r7AtrBRAsUIGQZCHvWW
0TCHdG6z9jgWO1rk1o04kaXRh/WFDtyqbmf6dtyNN964MukqueO8L4ccrwWPur7NvP7b1D/SNPld
OX053rKtgzbfjsGpQ7/FElwFfOQRXgljDsvdXcJCnMvXjwqjZQqzsBsvLsQJ9wFfIgIXmmosGx9h
etIOHQ6QG8umzi/qb0f7gwNwike3Mptn5XZ3VnTQdP8ikCyfrsexbPG+5DIVZ/A4m5C9suI3S9YK
Mq/B9j6B9YHdsL+PMX8OfXVkeh8zBOwCv08stsaCz2NZmaOD5hID5mss0zoRH7tB5BMQOddAgJNp
+yvC52KUOgLZyrDUhE0YLS/BLGyam6PjhQ84M5G7rmH+vU1PGHoEgFa+L9QvWwsCoPNeREH6MvIx
EJNs9a/qHfyLX/xiPUbXuzW5AqwioupKmSt6uFIdiz3hIyyzvgNtfRSd1Bu7Dt+8xo4Scznkdz/8
4Q/HpVnAq7ybAWHWjjzyyK1M15co8J35yzYH43KwTRwZN9I22lbbjMi4UjZOGkd3pxxDGDHyZEJ/
VS7Bu5p+Edgnvsz6hJZG83QoClEq9+N+aHIAnTlK+/Br3Hcy111iwoQJWzFtK2SlI1AgGRVHww51
+gikkGaK8X0EiSDW8XPH9pGuHB2jm85qp3PyHv4ayuZzLiyZsC8OUC7Ee+tMnE6W3htBmqzCAs9T
XJwJudagjnK08cwanjDeHwpxtzj55je/uSQrhBkv4SUEMe1g+nkRpu8XNjSFraTdsC7LPg1CbVsg
HrnYip//ODTfG7///e9vxIrc4XCHWRDAxCuvvLLCEu24WbNmVWSfntQJh3gyATYChHezi0gN2+3V
Fiqr7HZZ1V8/g3lbqbOLadpOdMjhuHatwLT0B4iVl0Uy7yx3voFyHNX+ctDo1EIbW2ij9xKFOsAj
XFdTV2CpufLiiy+OMwN1bUfc5zxUChEykVnCA0yb3wZO2lgWFr4Wl4rhoHKpoRUytWI42TAd8iSi
ur1Hbr6jL2hQyH7OOzmAK3aer/PrlLbPEdZXWfOKp3NCD2hI01tcQ5IFeow2w12EQVkfMKEzXNZX
KZijt08zoOACLlXT1vVN30eb+yqq3/EDDXRU7Cj1hhH8Atqt3r1ygKrOFRzpGq5aEMlw4sJ8yjU0
Y3SEVbmvkCc4BkiLq3EDGUCmx7lmeR42fcovj+YBq84jZg2IgICFUZ39B/LpZa5diIsK5xSuIpcg
RFvQD2Zj95hhRLNCUwiAM3yDALDKvYhMD3cdAKh6Fi8iYbTA4Plimi7Yd5Z5lbvuumuq70gX+WGf
AzryLTsHiSCdO9x56aWX9ovt57wLcn3qqadi/YEDKSMb29+d+hly/V0JFxXE36qeO6i46ElSeQmd
QKWwknGa4gfs0hQCQI4GALD0mUxxpttaRnUN+ScBrOVzb+5fUHsQSx4taOfjTEsQe/3iBipx/vqb
3sL7ExawTOvvJk878L/e8lN+iftfqb4CnowLCGAN/Bg8vDrwB86mM12dafqM05R3wC5NIYDUuhi9
ABGjGqqOaETA2PQ+U7lTnIhi2vcQdgORFInxH1yPhZuYNzNiC4Sl/MVF5CaNXCWubE1rT34CRdoF
uaFOleRQlFMH2OZ57juwfFh6tB9vnw2BdwxRNWBqYTeUR9A/bJoMs/eEYPkohGN9gAC8VEj/bNzM
e+UzJVm4S0+vLFze+eWKDmM0P2pCj2M3cEDCOnFTGtV51sA08TY9cejQNgimE4fPCrOGfU1PZ/Ta
VjvJzkmixFE3ghnF8lzFYhcjSO6xQOv2qS6dSRRP/tRdlpbQuHXmocOnClpPT5mgFBApAfs///nP
fXA4laXr2Nq+2mqrVVDy/mzSDHPKFshB/o/yOeMKHWhKet8r7Ond4LykEVnB8HGM0zqQEHN7DCkv
MSrDXJsQWgcAU6JwkkSBDOMRFraHSgkauUBwGcoZgcHk08yxf4ft4HG8ep/XMsd+u0sxzuyU8/eH
G+QZjHmYxXwY54xfYTS6jxW/pzE5T+J6LQs5h5T2JzbqKUUbmVradvdABCxMd/+W2lKkyTjQORZ7
gOIh7CCKATyNjzd9xmXKOzQuudEs7nwAypd1at3r1nsXq9uWQlFGdk6P4eMIjUGMsEAaSJ+N8WgN
06eR6a0hEM/S7y50+FNaDYmb66fnL9uzLgTRMdNoKCMKyn/5HWmHswjzm3GYZnsr07qwcj5D+vem
vAUR5DLY7bQ6bXcOH8SvDsQ2tmNMn5eBvc/pIVS8yzfI9XVq+saa+CHTZNx4P9ChVxY2EJXQ6PB9
x93qQWUfBhdZdSeOmh7d/nbquBntV9Ymiy5YIoriQ06dUIgcJdrUR8AS9fEvggiRDYO8XXEzu4Kj
VnznKqHl+Yu8XKuc+l3jtxedOo7f1rRhDlcuPYoqaSKkuCrXYTh83Pjb3/7WNnZQfSuyuZWy42si
5Kvix1/9wx/+sDIs+ypM2++HU1wpMZdnE4iM5Wh3rPpxbYXlu5/gXisr74EALxJP9bHHHtuWGYOv
HSjD1IUQiQ8akXHp/VAKweZAaCvGEBWfghWC4LsSIIVsy9wAEbCzPvu8FxE6eFQ52j006TRaCvaJ
mTVYLNYyO9+9ecrSPIq8dsNNjAsjDNzlXO4NxYjteYz/GAxwrO8ie807K+WVQIsyrYO6qhB1cChW
FOtEVB7RXDdI6w3W3yk3wdq3szVlWL0nBA4STgocMYN6SKLsSdK8/6IDmlCFSHPUV9lFc5/lM2IE
qAvlbmNk9iHci5zFBJQRE8Ay+pfTDEwwv+bhmcwG9JOLuTBpva1wxMzyxK/uvaeLOXOwHEIk8IoC
2ionYSoWcXCfzUxP6Cmk5z7/25YKabbVGOOuZfOiu2g6Lsq0DjV63hnXTfvWZe/gGPPS6UWHYfCZ
Qdo5RBtX0wgG51jMdMJqOdz6XEUkHoZ7+cbcdyXOp1Ho7lRv07g09fVQnzdNCoE4lKAwhlgHo6dt
2rRpFRZfzjzxxBO3JWq2gMIigxjB95rluTBLqzORx4U9PCGllWXVF1D4fg079YNOw/G6mcwegSdF
rNzAKytsd7Dy+ArlLc4ZxBV+Zyc4i45Kz47KiCP9RMoxenGUspkc+HCHZSEKokzinqSuJ9lSNpw2
t2GoudotYaTX/l/NBhu0/RfIE1Y828zsR8voOhYsrAmO2ewFfDe4mOi+BXED/CapAHd4A3MbOIzI
IfgXSGWx5Q4B0TnCK6w12CrLwB3jx48/mc+3FDIeEXC+aUCeNlE3Xf6Vq6HotDR6nC62Qwj76ovH
/XB89m4hneWLeLdoH+s3fD70oQ+dDMd5j4UQinJ6Huv+4x2K5dtR2L74+9//fnXE17Gk8CibKBMC
+LOu2mj4n4UwP069oVzmNpXLR1G827yIlICFFUBhi8CIX4LZxEmYv0PUkSZwknGEYivODPNqb0+K
QfofDRdByPHHaKMEEEBC5SLFe9murs/Xc42A7LudGx1GAmn4A1yUXtWxwhLC0+tKBWI6DdnsiAnn
UHb4blu87LnpGV4NkQ2PdWksA44QZeqPSKed1pC+sPKV4qOtTPtsewELsOVOlePc4Dt+nigSJ5X5
nHEkzlhZjDUC4oceEeQOYgSuzLQnRg/AeSpIIBOgPPpFRWrWOBQk5OAY8oyA8sNNinlwrJ/rWEGa
uqmTzynIdmOvf47A7+5LzN1/OWHChA8ahwbd7rSrQfHKyXu9mtZyzWsCHDXej43hMmwaR2e4dOpM
7+fqnDzNY/p5LJys8G+AQ71I/jbatmk6Fn8m4ksOEFyAqwQQgwLxNUPcWX+u0/shE0BgjCRdtNlN
GyMfpSyAY7VwFnIwE4Ks+jkBI8+62SWMx9ixAyvexXe5M7zvI8zVEaSrG8195JtfdG9l9FZXUU5u
K7OOXdJG1ehg2L37/99gQsTiE1wCB875sRmoMGozCBxBIN3zcW+3mEUOvQG3yIVagI4RXv/xj38s
lWz7fqLNU0ErALsvI/sAFKw/MCquAxm7mRany5XQlL0VCW2Mngrz4SlGJILxtq8gMvOaQN4eZjmL
GqqJe2SOIFzW1WfIbcVgNEUYCE47u10Mw4K4ihFwlA9ipr4RhfIvcJbxiJn9EAWagW1zFSWzFTtJ
eCllXJpvoEOdXB3owi2PDlwSAshF6xZm3DOc53MZkT/yBd/qi/cQwooYiSpM70RCO7joYooXy6H6
GGBoiXTz+MtrApV+pJ1HMfWvsoEH41P9iz6eclvx7HmJKa2Kr3hWF/BAiSAAHGbVB7a2CIxZFRTK
rZhRVDAIGRUHUwB/1gEirhl/TeMAICE4APJ9ZQw7tj1Go25OcIAYFowsAfQI2AAUWWco4HRVDI2+
aW0sKmriDTpPLl0jhY4hAQ/iTpi9j1kEM6IRWkBTqHp6Cnga6XPGZX45kNemITePFka0hqDcZnDQ
os9fsAQ6Xt0g/6T+p5MRyHbFejoiY5SZm8kGLX8gQ24rnbgWMt3RL4ytGpgw/U61LtIYp2dSUDx6
gLMBX0XgQ5UapWLQZFzmdwN5bRoB5EYygouPMTCiW//617+6yBFIQAYG8LJM02MDmIoTiNO/Voik
ynKqSAhtHjZcDKVc9mC95rZyFNzu2Ay0KtqRfsbebxQ+brszzEnpdVAUfgC+h1P4tfKAGaXSqKaE
phFAbjSKUKtEYGB0qwO04C6+pM9wgACQUSCCWiGIp9APtAOEKdYrCyTv8UpQlg4FIrCNrv+3Qrw7
2HAVXwPK4W0YpKZwq69BjO54wZ+Wwh79rydGgkBc/p88zAkH+No0AoDFReM5GWyap20RrKuqEoj2
H/NbCKAAh7lzLNBgcfudrBDfuXiHa/kollZDcQJpg54Achv5EMQasPHRAuG6gR3KyujVPmdYvc9B
ll9Sll0xrWCJzOKiaYTQNALIgD322GPOb4ugnZ/j2Oqo35fuIPKKjvCAHMMR5DMydGlmBaFF+jxU
At8KWIp2q+B5/lArnLDCNDiWgzOswpJnK55jpI6QgzMGcKCe0NTQdAJgntvFwoZAxOgFKX4rJzo7
A1+GkA73NK8iSrsAZ/kWz4P9Bg4QTbz44ourru8DT8CNaKvgnBLvsngsw4J9oCXZQIxuQVxUSKcB
qamhaQSQlRzY2rNQ/qtAISJibwDXtYUqOYR4WwRYYWjLOcK5MQsy+XHQXzMBcHpIix3O6A9ih+tV
7rvvvuB8Za0+T/EQfasnHcA0LeJM3AlwxmUzgG8aAYCIAJzFlGeY4oQSgO2/pt8708I9BQZ36LlY
HLOAkYwaX0d+kPIKGvF0I4ZSQN6/gPavI4qEH8vBKIOva4Qh44nl4F0VA+LINOJM3Hmf03g/0KFp
BEBDBUSHkG6AudGGeyyrV0a+GyHHcRs7dIzLAQIZjTNEsTmC/C+xRv+S75uJiFz/ol6zPwDbul5i
2hvthqDDIYRRvWa5fOAR/91uNcc8sLPvMo7gfDeKO6LCiui7ZoRmEoAyLNqM08YlzH+d6kgAXXjE
tOMuvaEvYYfRhswWIYA1ZYWMlhgJTIWeYQ2+GElR4CD+o9Nsdwvew6/A1oPzCYswwfnCISTDmmFn
xrQF+oId3QWOWsXV2LFjLxHMjEPvmxGaSgAAGCwej9fr0IL/AQAC2Z2mO6MbAIq0jIDQD7LJFE1Y
f/wYBVyDgzTkG1SPqY1canK/WMyBAwSe4WxjU2MDVggi4EE3eJ0KIDAb7yHU/xBnps04TPkG/NJU
AqC1MRpkZYyCLMdrsEJ9/cIGnpAgImr4Ci7JNHENocRopF2gE+Ssxdr7kcaxYiYhDOqQ24hz6ZEP
PPDAWjS2M8GiPWANvI01ggVe8pYwpnzDIPxC7DENnp7Yf+ClmQA3lQCSjKtxIsaqAL9BAqTNRR6o
PBYInBOTLkbCddddtwoEEHN+kKZvX7u+cn//+99P/PnPfz4KhHWmMhcEJ3Ga6IJwD9OmeqJd/a3M
PLaRE0hGQcwnOqqFAfYf5dDJK+Oivqrlkdal6xBzpJkJHpT/0R/MGNbnJBSNZXKSBWpDf9ua0zWV
APKiCPL+DXjFSvmyuPCxhyAetxGunZfSrQhHcJTrnx/et1jQum6//fYRGFbOMb2I89qfQFrhcxlZ
l/EaCG+fD0LDn8C05HU65rXfOMptg22fg88fTR/RhbhrSQTgolgbXGFF2y7M2W8AhXGy836CXK8b
K+jSKIVvNALzeL/rN/2ChqYWnl29p0yZ8ua06UGktrE83I1m/4iNLc9xmTYtr02cIOV7VRRIEJ04
ju7EMe3jue/Oy8e+7yvYcXYiDikj2GK1sR+GomNiT58uWxID5RSuX8mNK/wJqLv9pJNO2tCrZVhW
X/Xk+NSmbpxUx7PgtRPxsv4QWQmWmmweI1cQgLjJsLv1DEORZsAgAKeDmILfYtkZh94PxRAIwBp4
AY3XW1cg3VZV3vQgu410uIl/JH1mNc7MM60/kCbn8IMKszhebQz3lXnt88sdhofRSDxvHjIvxqTn
OXLtMPP2FeioFjyMD2aTxmRmLh5QdS8yO69D9EkEuS0XXHDBGA62dMai7T/a7L3TQK6dwiaM1p9h
9t5A++7hUuCIdoszQ+Cm53bg/5taOM0VCS7shB9cbj7etbfKZnm2fg+JiFewyFeTPdyNExHHnyPH
5eEuuMBiF1100S+J2xTWGH76qZycNq58M8CRVGUUf+z6669fl/sOFlZWgP1OZHp2CKPtYrjQPUzT
nkX2Ls6y81KM1o1w9d6TNGtrhzAP9ooNEFX7c//1XKYvykGiIQScl1122S8Z/YvZVtosbAKRYWlx
HQRRGLpPhpn3gQPacy/3G6YJgwrj+jwbouye24H/bxoBJMTU3PXKgc6r2XSAatEmzojWHcpVsRY1
4SwLmRlMdb5MCKShHHlgZNjIURztVE/z2ARny+/iMXsIiDfOZeJeg9p1etGFu3g7G1Kq/NYm7jiO
o4s1dzor3K9csUxE58GOLRCj5Q6HSHrWcnutgYamNsA5zuHgq01I1kk5Kn6xng9hOetRb2lxjZ/B
ECt8GeaMA4jjDuDdhwEQOg5EuIqfluNouVkZl300YZGi+2Rri1QqmWF1AQi2/WVh/dnXqcWVPmR9
jAIQHcM8y0L85p/AAqaYaAXxHinTAhuejIzs1IiEmTRGC0T1GQ5UGk+66KTGtvoVD+M4dets9hzI
WpeAuITVvXeeytmBXtKFs0aVRZhuiKILAvPAxvDeVVkzD3L9HnwXzua+ksv0PgfaLXF0QeAHwWkO
8B6Y2m23m1446PkpCMB2S9WtOITMQEQ8bn7ERlB6xgHpngdmXwXeINjl0QOWNyLj0vshE6RaG+sn
1LHkaQPQI9hjX4t970nxyjAFMeLT/0fTghi1QfWFW/DLnyA34Nkvc4q4GnlrmJO34V7HkrlGadYD
aMew8WwYwfPGnb6WUf5ZVpSX460HWW3nnWZe4pXXcw2UXCdt2B6F1jKrEi1X3d08EfRkxM1fvafN
AQunm/6eZ4MEFiHjgLMMjodDFHsIsCc8N7/TS3MZg/kaRIBSdSeNrEHVgQhG9W2p0fHee5Ac4ghF
7ZN+wYOoQKTnCSB/V2d7l6bR+GYfiFYuqtjNyV8Ud6MGcXWh3HF+I4BNI19lq9kd7LqZAWut6o7l
z3vjfEfHf9W0uaByGTku14VtYn08maOdqU0BH99CvBidY0z6KknAwnqI5wMcZBnOQHJZXAMHEIs4
KYiFg7TFmaHAUc/j0PoPQPF7n4CsjM6j+Z73W/3GN76xjqBkBOer8XRKHpWdeBDV6JQdTAt3eIqL
mnIn7Fo275dCn+Z0sdAxcseYNofEicoIr/gtX0TDumzS2Myf96Xv++asvdoMch233XbbaLjT0yRW
4+/iF+KDjaKavCu06122nduIhxiqTGPDISDDmq9sAFkvbSWXAwRBwREmWA6hru09UUPkHzYZrA55
vWkaDcEiUQRrfHHjQMHIoyGLDLdDORp5FSPH/XjM42PkMNo3YsTF6IcA3E8XyCJu0i18lt3yQGqv
CBPZid3Oa0TN87zhXLYbO+noR22jbcjECAd5mangmrYDmPdnZTtgMB1H1szMU8oMa4adL6EfJE5I
JzzdbhtDf4ktyhmHxA+9kAH1HF7m8DFaoPA5cgPlshBlGWgH+YwL2UhEhA6B7sidQ/oaXxo72ncG
Fknek2SuGy0KmYt8vRfr2RKmyaPU+96C7bI+kevP+9zW3tIbl8u0Dka+Uzbnf57pH9zKw57Z7LJ5
zs/HKA5KR+OEWADOqa51+L5UV4gtONx3jAbWsJOgKD6JgrpYQ1ofh27AR/4BWi/SAkio/msJmkBC
RgqWu2UYLQWxqJRBLN9MaaODcZT4RNpz5+dc7YCsZN2LvtAvIkjl9euSO9+FHGYq0fmO/KSQdtO5
nvS9h4VxFGzUz7d+8neB/bCFo3syv6z8ZS4UsKObCF+ZAO7neeiH3KlMeZZihEwDIhExxy3cjPJD
hTCzQW4zUrz+I2nss9WMkaenmjZxi0AaCD8u7bB1l7HWthAHpJ2MwWiU6Rndc80OjF+QkMv405/+
NBrr3GPkDTmdOr9LiyHfLDgglemnY0MEoVd8igUv4e2TADLsEM1nsY04OIKQ0ZEev/rqq0ekMjNe
0uPAXuaa3gxk8Xn+isK0MvaA7NnbyhSvgnx8slwXxBKPGD+Wh52v5JoARABOWjwwsdxOlSpt9adA
RCeNGzeunbL9skg7RNDJtHNNOMTf+KLYmzAaSRTDMiGW65vffcozzDIsC1ju4rMxYyDITjrLOrvx
+Wtj9H6Fr5r/0Dbx68wWPtru8q7VtDDHryA2loWtL21EhtV7A6LiSYjF+Azn6oiL1X0HQQ1dAsir
fBhGXo+FSwSpubdJACh3jwhgtohlYsEsOxwrWoxcEQXC/cq2nV4E4lUE/UDDCewyOolZgx1SlQgg
mE5O8FoJq9xdKFK7k66TOFf1YmQWhczjxrTmMS/t2p3l3bss086nY6MutPZ22PxJnBB2Iulk7WF8
YlRzG6d8tmDw8jY6EKJZnIWdkOsZ1gw7g+ExcUKIcoCljbSxfA5BZaLw/dAKmcVhtz8SWV2wODaG
vMC0a0WhoTMDQfl61FFHrcLIetlXjKBQArGqHWTaXJ73KX3IVBSuT7v33mjq6UJviCmiiy+YaCea
PoW8bTw/111VCIkoCAXWPjGJmZjqZW3fr5pAXF9ImetOIc2EBof6JNzJNoVowmr4Mu9iYSnDmq+I
yBVQbF2ACJgVB+DsGMsvw+zzUAuBzLSyVZyVg5J3VwKkzN6C0tH4V8VoMpP3gTxGf+3ggw/+pOkb
kZEQGHXQIXuojJkPAsjaeSz/ImPvw76whWWkEOf92uH+UqcFMfmeefkWGJ4mQYCWp3ipMvKjI62D
9B83HaE3W0GUA4fYMSl/cq8acMyEoFY1E/nnGtWIiLtNR+eHkowuc75pCQVB9jwOrf/oYC1sNLsA
jrNzfp7AKIDLSOG6QRrNavdxYiZHrbzP9I0EkMrwErx24sSJb0V/eI5nO25OUtSi4+AicWKoRhcz
9Bb4mOUGiJRL09pEECCsuZhl0EkvcwD0Vilv1NlYTm7j+PHjd8Sx0zIkgKowCZvpM6zeEwIHDJIL
uRdHsZzMILkx3lYqcxFLih+QS9EBA1JaqRBHp3JUAw0fUhrpKxdJGJ26ScU0h9EUq4G+y/oCO4eW
8iQNgshrRQGsYKqNiCwzfdkQOp0h8DWS2zHSrIdCdTl29HfqTgZCPeuvG6Wqhd9esNq9MCxdSZrf
sm/xMXUMRM5Y3Lfew4GUO5PforXrx5YuFoZclxjOMTa3AcdumJ+fsi5dvxraEI+5jRi+Xsb+YZwd
WKP8FhZ3wg6QYfVlxgE4uc+2uHhkPErwSHDocXISoHHiY8BD0wggKTo13MGW19PHlusa5XIwCP2X
z1wLoEqeL6sCdCTnrx2kaPGL9I5MtHLfzRXsEOfrLqLwcis661i2oJ2CP6Ewai9Qb+jCM6idn6eR
7oyyFeWwIliRWAxJzrewbGvb2tFX/PDTV88888zj0FvCIHTOOef02vnmz22EiJ+jPqMkgC5gaIUj
rW5ECdYCB6Sd6uEZtCOAx59gGdovwbxsOQknZh/Q0FT2Ykuh7OwREw0XEGRjIW8boYEDrOY2qkT5
IuhFTL3RO3nTRWOe9NxixyT22sKHqb6KZ+4GnM33R0ZjOx3axpq7Bz66jtABojuZmXT5894435mG
Xxt2/Hb0gJshqk3tfOtAWWubV+eX24UCOp01C5VZQ5XlaDs3CKAnqv6ftDEYgTsIgKXjZckTy+h5
1lCfY/A/BSBQ73DPvKO5atKh4LA4lDXzshwNBIB0TcQelxZpOUTqbwnUKC/d112S9h7vlcHUGTb9
nIhjZfdAEbzdWQFx5Z/s1V8RhzioYee/HaSHZS+VEbaEUj36G/Y1eDLc7cjxRyybQRCwsMDz7VRe
mfMGDhBL4iSWzL1iOHtaZxrTC4/XoRhipOMTeC2ND9u+Vzrp4RJQGbhIyykhvzQNxBLKEHkvSoCX
kZai6hWqUpnxPnVY0VFMzbaGEM7C1v4A5+/MVknz571x1H2WaYrCYd+O+tJz3e08iCDDfT0ZioMi
ISxhM+QyM7G04I72MPGmDWsgy8M3RMoeEZJuh9gla8RYyw7X/Evz1cjj83FM2zYXnNRJ3kYH0xFX
cm/aGchNF4I+50sVL6/lkDuAFcT16djbUNAecREmpcmEVSGdZReE4HvyjMa17E3+vE958sURXia4
yEubD4FgHqBz/vyjH/1oAxOTrq5c43Jb8Qs4nseAxWuCzSRRdoadncSbp9VSDVyuMfiF0YA749BM
Qy5k5OBgsX421ABEh0ufTJOOEqCMrIwMzt/fmdNCRZoOH5PYVLKc6RpHt3GEGEnJwSTyoLTVmOq9
w5e5TO8N6bncsT0v/u9/LkNRLuP0009/C+Io6iB5Dd3itpQtj+ailAy3S9t04D9Mj1eU08CdTZTL
zLAzQI5C5Ft2TFn9cIQ4M20uy/uhGDKLa91uu+0CEbC4LA9/nAAqd0iMJmT22ih+O2UZ2AsSWg49
9NBYLNFbiDm+I0eDzWxEhyJmV8uex+iJ5WDLzT+SFxzDvDnkMiDGXVVok27SpdMKXGCM6WxLI4Hm
NiNClhIWYUplljlGwI7V7ye8K0SFuKK8nK7XdqWyhsQly8NfC2SW7czHf5Va3ziCMuDxWkSKXEdN
6owywVQw3FxuucwsNP926UKGI+fWZs4jzfuFDbkMy/RLnpSjA2kn1xri44qGcuM0UfPY5kwEpTR1
sBEfsKMsXmV5zARC70EXEVeGRtz0xA6x/+gwtPsf0u4CSCxftyQ4CgrPnczVxSBHeF1np/SKgxHI
4w8g9+/Sbk683rhyAT1vpvJ+MdPaCV4XJeQyLBNfgKmUpW9jcBzv0d7v1QydHTh6qUsYFhMmCVgY
S2mifcwO7rCszB1ZShZXhl7h73k1MP9NryA3E0fPoH5GTwCNohOOE7wHt2E19MAkETtXuOKKK5Zj
jX8M9oGNsahtTUdsz7LxqMmTJ5u22+Vi5syewTcc/eFEptKzRTbXWKGbq8AFiKCM2FNomVgav4Q1
78dsGHGfwTDq7Gan0gYsDl1y0003TUMvuB5jzg1YGe9E459CO1+kquBMDQas2PdAmdESV/+8SXsi
PE2skVNEumb8NZsA3P/Wiumzwo6baD+GF0dshRGj14+h+GI3I2lL/PU3BbGrYQlbmd9INpOMRKkb
RdwKmog9ZCqFbrVlTKYeO9NGJwxHOz8DWfsD3reyjLrInZ8rSmW1stZwLvL+TXyX8LO0XyOX3w+q
0eYWfpq792WZe1+sjH4RZTqK3xQ41FRmM9NQ8jwyZgr4uBPn0FtS59vReepXWPuwngZliDu2x5M0
lqZzcwbv1ZGc5LRTtjoqZj5/XQI2z++zTTeUOfziTtZQow3edL38NNh0gNA5pHGuHHLYdGrnOGEe
zr3B0dUztHqeB+Q/lRnl4nByOJ0bIofCbatTNz9cZbvU5OuMSymN+k/NRSmWqCcQZwjYURLVh4qF
IFzCtZuUg7gM/aIZsJUrWpj7vLmzLNuiHL1njz766DVRhnbAGhh+fowCkeR699dTZeH8wdTnbuMZ
La+oaYOs4scIMI8juiAMXa6ZRr3IfPnH7BdcN5U1X+fOlG6hLgn5Qdiw/jXHjx//Eyx2M12yLrXN
zu8EjjnCAMHOhjDiB1yvmC7BahsCdmA4w/iMGxTkqeBtBzaXrKtDrQkbQj66viF6wR8XRQQ42ttk
j3RwsFsdGzj6fWu2gm+DvN6Edfw1kW+raFtHdsYJGLD1Fhc9WPN/LDVXJHQgvx+E3W3EProlPEGk
HBhdHjFfYeR3s9r2OCz2rxhkrmFqdg2K4HMXXnhhRc5jWxJrLWcfsPvEikMnQPl7hII/jp3iaBac
dkMv2AmP4bcovlhBbOfqdwJ7rXudddZ5AFh9F7CjwN4vfIg4qmipsVg1El3nt3JDpsLPAec/wddd
nLf4B5a7b2KW8CywRtkJ7syNIm5B/haKVdLh7bnTHRUs0uzy8MMP749Stg1IWDadBVBuRzfU7dl3
sbzplbnuFPwC9uPc/D+ZkBMxRrG/7vvoC+sxuiWo6fyehlieQjuezEiahJx/GPn5GEgqU4htcQ+/
I+/fFqgvbAhUWOgaLn3jN7g2+/tfD6GvCxGMZnl3FB27HJ28EnsQh9H5D8I1DkTfmWZjTzvttK3h
Juex8jcOroEeWG1FUfaDEXKVOq6KcljBy2gmLmS/hev8CJwVooL2FH1iuU0JAk3BBddgtW1/PFce
SvPjzAbdfDnHKQ0dlWV1lom6a1UBNCjW3TDu9AVhsehhoyGoovx5ACGwvXnjzCNLc16Jk2TRq+us
3morwybMmLm/my2k+iyIG/JlUee95yQELtV9fOYXeFYEovTeh33iE8TlIF5CROWIAbuK8FzYhAkT
PsDofdhdO8T5iw9AIfPqZDVE4L67mgTCgstM700PoJ2yc+6DEHC2mIpz5T4856ASKUL9DRPBsroE
3EJxLcr5d4Qw/tjWRBTirIAjN0BYYetTeRYf3eDJzo/OdTMJPg0z9UUUX+IwpfPahbFIdzcVzSAU
/RNRsO8DNzsSF6HcVzmur2t/kWmHdMKeV2CZ9HvIqA+zgcMyO52DI+tqsLogECjTb95OI/4WqPZW
5sQPIt+eYiHkSVjk3vjXf5szf5R9nUyN2jgwQSIYhg7gKWC/R6P/HEeqh4DUwaO/6+82ZjCGMgy6
nIGDiTivvMtvIRDEXxuiInDAOkYH4uEwjEWXgqdRcInVOSfgDUydN0OMbOFUk51TASacwUHUir4h
pxiGabpC3ouwVRwEHl8yjp9EtfAhab3B2phfb4cColyWEh3xXcj1giXRwbNo+Pl49WqkWbyvWs8/
//w10Hp/5zKsZVlGsuJFWU6TMO9ORPmJRSDLcUT1Vd5gjS+PQmGB3U8UNtqb8adeFDCLC3EibvqC
h75YnJXO7TA2nccUNNZTLCtxU7lBlIVu5VfNt0nl1Hksp7j+XShEWRIcAjlzVG488ts5rxQbLAiH
z04afyqa+GoNJRd2ccuSmBwNOQ2Esj+NlVJFiC5bxQ5b43DDeh5qPoJ8Wabl6WYuYlBeE7EWnNVz
iVjrf16Y/MEVs/jL+HvJjaQZGHEkrsSZZuNUXt0AYLa1OrOQCeBIkRv4s09QHhUNNaaRNeqN1VbL
TX3pbf9COQN72s9w+xM5tbx1I+ej4VIt9n3ackmZasPWLQB91ZTKjk7FTrASu4QvgH0FcsjTkbhB
AMJ0T/n2T4D5cKk8te86hJTevWa3ycYf3NJGIMZ2V0Eu6Ul54ARsyni+FHYB3GGl1Og8q+gVBnGa
6ihgl2NgT7mEmUHgz75Jg7PbMxbwpj4rF1YaSDmq92u58xmll5HKwsNJwavPUPQrcIU6l6lyPtLM
N5S5AT4A26nI6CdAxtjnlwCJ+pj/uv5+F2Log6WCBwVHSMRYdDzOqO/CP+H2LOJor0qbjq3BooUR
BfohdzlnWMq4yHHzuiZcF9zUvoATh5HJ+sBd9JkKJJtwsxdSZb59lEZujE4o+CIoyoLmMCqjQO5d
/brnf/7nf0anBmbNPD0u2CU1qKBo2RYsb6b18PMrWpldBvI0FUN8N6CIvr9ckxr3vLhOOe1A3FtX
I6sH0e9iNnOD0zPq8KeelOW8ipom61fp7C+U2jpg+LNP7JtUt5zUNoSIgIu7ZB6hVHeOqrtGZwDM
N4iNAhI1RUHI+l/l1LAjNfkBCRBCQQQeooB94Yd0akZkTJHUEagsCEGRAUe4E5v67g0NcHdu1hka
Xi36o2Wn6V1RGNxrN5TjG9NZALZZx5QuiNf2hrjUwwdRdx4Gn5E5YxnmHLew13JfpD6q6zuMaZ6y
8oNUfsGt6urLgHniVaJiXaUtKFy44AiKgwgD2fhcJtc6T16/scu6+BUl/UAjUifzYIkgCEE5x3x6
EgTzmfKsgfex3Wt+1F6qe163ebQXREq5I2C7n0DG3530o+h4OKZGMNsWHW/bMVVfzba0t+QKmsWt
yn1iXyVxqvgJYlCMYpo/JrWjEB/xnDPjh7ZlGnlVEO2XvkMGY60rOh+lr3cKyhAu4pW2OIILZEOQ
m8Fur8ybNHnXrWiAMxUcQUULR5DnnT6ydBy+dKVmLMwqWla46hCl0gXXOYl1/idLC0BuF8+EGR0v
N0Ac3ITOskOpHU3lTtZT7pvUZzGA00DuctMKusf7TFvoHQnhfphpOaZlz/JO61Ocm+c9o/BPZjDk
tD1Pzf1v1HhxytyeQ6Ku0+WLmuOXpqSOuCBU4zUvo2Rdz5z5o8wyYmNFqaXKXH9ziQm5RW9TLuLb
Ge07MpIvxtCiWTbXr36Sp67R8WrjKGM34eRZKHjUHRtQS21o6m0ZttR3+Rwjp+4OlBc5fGJlG5HT
BjJAuAsLGmZccw8Wi61Z//2Q9WXqMvO/KzQSgh6/yjmpmTbkn0pPZ54HG+8odP6NrvATdJodPd6l
oc1OVfOy6lxc7cgjj3wrrlqnMtofKbF564u6sMQVRGdbaOdNsN5yx79m09XcV/Td4gyGyeJD0emA
8R53vKzL9cANsIenTY15riolz4LljSODlFKwZJ9fi5CAKjoKjqCOcBbGkBkqObTJn8qXck8CDiI2
3vOEsJ79Cy35Ijx69vMbPcTXBXUI3m2PsvYdOOEkVu1ymUW52uHJVJQLR6hCJJeAny1LhbUmTlKK
+vff5j6zD+1LWuAZhHKBDk8vw8i2b7QKf7uxsIVgDygOgTi1Vv3zTVDIikj92v81cgTcwlcaP378
YXjR3N/QaR3Y2WXTmk2L0eoCC1bN2SiPt2OIORkF8vPYIK7GDv+MIx0Cio5ntHSDjw7WK8RJkV9i
grM8hYz9BqeQrFPCyKDo+FJ79IoO7m1fJo7p2kPAApG/ECISa951ZBLoeIEWq8tS9tapU4LKhb/W
91B4nbJoe1AY362sZl1Cpaw8gvu8T7aO8nsHQ8h0rjF/dyqMbtGFI8Y1GHr2avAAbvT0tSmDKUQf
2qcQdYYzuJiWyBZY498ZJSPZmrQSxoQpUPfDeLC+V6cLFSOCmQZtsI2sfrWX9+tfwLn9nA/wcVYa
d2eVcjiraVUPm8bBNL7PyzkFNX5Vt4DrpGoZAOjKZBX9Yg06fGk8maq8ayX/JMTjgdUdOgAAACtJ
REFUT1m3vwSb/SMlRKjVd/OTSAZtyH3I1Y9zX0vf6so2mpXd5+Buz/4vKC+h5g2ysFcAAAAASUVO
RK5CYII=";

        void ReloadWatermark()
        {
            if (s_texture == null)
            {
                byte[] rawImage = System.Convert.FromBase64String( watermarkImageBase64 );
                Texture2D tex = new Texture2D( 2, 2 );
                tex.LoadImage( rawImage );
                s_texture  = tex;
            }
        }
        
        void ReloadBk()
        {
            if (bk_texture == null)
            {
                bk_texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                bk_texture.SetPixel(0, 0, BackgroundColour() );
                bk_texture.Apply();
            }
        }

        protected virtual void OnEnable()
        {
            ReloadWatermark();
            ReloadBk();
        }

        protected virtual void OnDisable()
        {
            s_texture = null;
            bk_texture = null;
        }

        public static void ShowWindow(bool immediate)
        {
            if (s_instance == null)
            {
                s_instance = GetWindow<T>();
            }

			s_instance.Show(immediate);
			s_instance.Focus();
        }

        private void OnGUI()
        {
            if (m_errorStyle == null)
            {
                m_errorStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    normal =
                    {
                        textColor = Color.red
                    }
                };
            }

            try
            {
                DrawWatermark();
                DrawBk();
                DrawGui();
            }
            catch (System.Exception ex)
            {
                bool wasError = m_lastError != null;

                m_lastError = ex;

                // #JD 17/11/2016: Check is to stop spamming the console.
                if (!wasError)
                {
                    throw;
                }

                EditorGUILayout.Space();

                using (new GuiScrollView(ref m_errorScroll))
                {
                    GUILayout.Label(string.Format("Exception: {0}\n{1}", ex.Message, ex.StackTrace), m_errorStyle);
                }
            }
        }
        
        private void DrawBk()
        {
            if ( bk_texture == null )
            {
                ReloadBk();
                if (bk_texture == null)
                    return;
            }
            
            GUI.DrawTexture(new Rect(0, 0, maxSize.x, maxSize.y), bk_texture, ScaleMode.StretchToFill);
        }
        


        private void DrawWatermark()
        {
            if ( s_texture == null )
            {
                ReloadWatermark();
                if (s_texture == null)
                    return;
            }

            Rect rect = position;
            rect.x = rect.width - s_texture.width - 6.0f;
            rect.y = rect.height - s_texture.height - 6.0f;
            rect.width = s_texture.width;
            rect.height = s_texture.height;

            GUI.DrawTexture(rect, s_texture, ScaleMode.StretchToFill);
        }

        private static Color _DefaultBackgroundColor;
        public static Color DefaultBackgroundColor
        {
            get
            {
                if (_DefaultBackgroundColor.a == 0)
                {
                    var method = typeof(EditorGUIUtility)
                        .GetMethod("GetDefaultBackgroundColor", BindingFlags.NonPublic | BindingFlags.Static);
                    _DefaultBackgroundColor = (Color)method.Invoke(null, null);
                }
                return _DefaultBackgroundColor;
            }
        }
        
        protected virtual void Update() { }
        protected abstract void DrawGui();

        protected virtual Color BackgroundColour() => StylePalette.DefaultBackgroundColor;
    }
}

// https://docs.unity3d.com/ScriptReference/EditorWindow.html

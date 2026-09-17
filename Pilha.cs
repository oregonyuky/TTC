using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessamentoImagens
{
    internal unsafe class Pilha
    {
        private No topo;

        public Pilha()
        {
            this.topo = null;
        }

        public bool isEmpty()
        {
            return topo == null;
        }

        public void push(Info pixel)
        {
            No novoNo = new No(pixel, topo);
            topo = novoNo;
        }

        public Info pop()
        {
            Info pixel = topo.getInfo();
            topo = topo.getProx();
            return pixel;
        }
    }
}

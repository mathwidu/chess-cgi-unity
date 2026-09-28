const people = {
  knight: ['Gustavo · cavalo', 'O moletom mantém o contorno corrigido. A sombra dura da calça ficou mais suave, e a marca foi integrada ao tecido.', 'Contatos das mãos com a montaria, relevo da montaria e acabamento da base original sob o cavalo.'],
  pawn: ['Matheus · peão', 'A camiseta deixou de invadir a cintura da cargo. A mão da espada ganhou dedos com variação de comprimento e espessura.', 'Gola, transição de manga para pele, barra da calça e anatomia da mão da espada.'],
  rook: ['Alex · torre', 'A gola e o punho direito receberam uma máscara que separa camisa, pele e jeans, removendo resíduos da cor antiga.', 'Mãos entrelaçadas, borda traseira da camisa e encontro do corpo com a torre.'],
  bishop: ['Rafael · bispo', 'Dedos do báculo menos uniformes, base suavizada e marca sem fundo retangular. O bastão continua afastado do braço.', 'Pequenos planos sob os fones, aspecto da mão e resíduo da base original à frente dos pés.'],
  queen: ['Marta · rainha', 'Marca integrada nas costas e base mais uniforme. Lenço frontal, óculos e saia continuam definindo a identidade.', 'Punhos, barra do cardigan, trecho traseiro do lenço, cabelo e contato aparente dos sapatos com a base.'],
  king: ['Ricardo · rei', 'Marca legível sem placa nas costas, dedos do cetro menos uniformes e luz que preserva as dobras do moletom. A cabeça continua completa.', 'Contorno da coroa original, acabamento de rosto/cabelo e anatomia da mão do cetro.']
};
const $ = id => document.getElementById(id);
function compare() {
  const key = $('person').value, side = $('side').value, view = $('view').value;
  const [name, changed, remaining] = people[key];
  $('person-name').textContent = name; $('changed').textContent = changed; $('remaining').textContent = remaining;
  for (const phase of ['before', 'after']) {
    const path = `${phase}/${view}-${side}${key}.png`;
    $(phase + '-img').src = path; $(phase + '-link').href = path;
    $(phase + '-img').alt = `${name}, ${side ? 'pretas' : 'brancas'}, ${view === 'detail' ? 'frente' : 'costas'}, ${phase === 'before' ? 'antes' : 'depois'}`;
  }
}
for (const id of ['person', 'side', 'view']) $(id).addEventListener('change', compare);
compare();
const steps = [
  ['01', 'Menu', 'Boa hierarquia; acabamento mais consistente', 'menu', 'A marca, Jogar e os dois professores permanecem claros.', 'O modo desempenho ainda pode explicar melhor que usa peças clássicas.', 'Manter o acesso simples à partida; alinhar o ambiente do menu à futura sala.'],
  ['02', 'Partida', 'Volume recuperado; ambiente ainda simples', 'board', 'As roupas têm mais volume com a luz do preview isolada.', 'Mesa cúbica, fundo cinza e borda lisa não comunicam uma sala de aula.', 'Dar forma e materiais à mesa e ao tabuleiro, conservando o contraste entre os lados.'],
  ['03', 'Seleção', 'Informação útil; sobreposição ainda presente', 'selected-white', 'Nome, tipo, casa e miniatura ajudam a reconhecer a peça.', 'O painel cobre casas à direita e o preview disputa espaço com o zoom.', 'Reservar uma área para os dados sem esconder o tabuleiro; fazer um arranjo próprio para VR.'],
  ['04', 'Movimento', 'Turno e histórico claros', 'move', 'A jogada e2–e4 e o novo turno continuam visíveis.', 'A troca da câmera precisa de avaliação em movimento. A imagem estática não comprova conforto.', 'Verificar orientação em desktop e manter o ponto de vista do usuário sob seu controle no headset.'],
  ['05', 'Vista superior', 'Símbolos inteiros; leitura à distância pendente', 'board-top', 'As bases distinguem os lados e os símbolos ficam fora dos pés e da montaria.', 'Detalhes finos ainda ocupam poucos pixels; faltam coordenadas. Destinos dependem de cor.', 'Testar símbolos na escala real, acrescentar coordenadas e reforçar destinos também por forma.']
];
for (const [n, title, health, file, strength, risk, action] of steps) {
  const article = document.createElement('article'); article.className = 'step';
  article.innerHTML = `<figure><a href="after/${file}.png"><img src="after/${file}.png" loading="lazy" alt="${title}, captura final no Unity"></a><figcaption>${n} · ${title}</figcaption></figure><div class="panel"><p class="tag">${health}</p><h3>${title}</h3><p class="notes"><strong>Funciona:</strong> ${strength}</p><p class="notes"><strong>Observação:</strong> ${risk}</p><p class="notes"><strong>Próximo ganho:</strong> ${action}</p></div>`;
  $('steps').appendChild(article);
}
function showRoom(view) {
  const path = `../classroom-discovery-20260925/classroom-${view}.png`;
  $('room-img').src = path; $('room-link').href = path;
  $('room-img').alt = view === 'eye' ? 'Estudo na altura de quem está sentado, olhando para o tabuleiro sobre a mesa' : 'Estudo da sala com quadro Feevale, janelas, carteiras e mesa de xadrez';
  $('room-caption').textContent = view === 'eye' ? 'Altura de referência de 1,20 m · render offline; alcance e leitura ainda precisam de teste no headset.' : 'Vista do ambiente · estudo de proporção e composição, ainda não integrado à Main.';
  for (const name of ['overview', 'eye']) $(name).setAttribute('aria-pressed', String(name === view));
}
for (const view of ['overview', 'eye']) $(view).addEventListener('click', () => showRoom(view));

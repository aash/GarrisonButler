var g_missioncalcs = {};

function MissionCalc(b) {
   $WH.cO(this, b);
   this.DEBUG = $WH.isset("g_dev") && g_dev;
   this.minItemLevel = {
      "default": 600,
      "5": 630
   };
   this.maxItemLevel = {
      "default": 655,
      ptr: 670
   };
   this.maxLevel = 100;
   if (this.id) {
      var a = this.id;
      if (this.parent) {
         var c = $WH.ce("div");
         c.id = a;
         $WH.ae($WH.ge(this.parent), c);
         this.container = c
      } else {
         this.container = $WH.ge(a)
      }
   } else {
      return
   }
   if (!this.mission) {
      return
   }
   if (!this.qualityConstant) {
      this.qualityConstant = {
         minimum: 2,
         maximum: 4
      }
   }
   g_missioncalcs[this.id] = this;
   this.Initialize()
}
MissionCalc.prototype = {
   Initialize: function() {
      this.div = $WH.ce("div");
      this.div.className = "mission-calc";
      $WH.ae(this.container, this.div);
      this.sideDiv = $WH.ce("div");
      this.sideDiv.className = "side";
      $WH.ae(this.div, this.sideDiv);
      this.side = "alliance";
      this.sideDiv.setAttribute("data-side", this.side);
      var h = ["alliance", "horde"];
      for (var f = 0, e; e = h[f]; f++) {
         var g = $WH.ce("span");
         g.className = "side-" + e + " icon-" + e;
         $WH.ae(this.sideDiv, g);
         $WH.ae(g, document.createTextNode(g_sides[f + 1]));
         $WH.aE(g, "click", this.SetSide.bind(this, e))
      }
      this.profiler = false;
      this.profilerMenu = [
         [0, LANG.som.all, this.SetProfile.bind(this, 0)]
      ];
      this.currentProfile = 0;
      if (g_user && g_user.lists && g_user.lists.length) {
         for (var f = 0; f < g_user.lists.length; f++) {
            for (var c = 0; c < g_user.lists[f].lists.length; c++) {
               if (g_user.lists[f].lists[c].type == 20) {
                  if (!this.profiler) {
                     this.profiler = {}
                  }
                  this.profiler[g_user.lists[f].id] = [];
                  $.ajax({
                     context: this.profiler,
                     beforeSend: function(j, i) {
                        j._WH_list = g_user.lists[f].id
                     },
                     success: function(l, j, k) {
                        for (var i = 0; i < l.length; i++) {
                           this[k._WH_list][l[i][0]] = {
                              follower: l[i][0],
                              active: l[i][1],
                              quality: l[i][2],
                              level: l[i][3],
                              avgilvl: l[i][4],
                              weaponilvl: l[i][5],
                              armorilvl: l[i][6],
                              abilities: l[i][7]
                           }
                        }
                     },
                     url: "/list=" + g_user.lists[f].id + "&tab=" + g_user.lists[f].lists[c].id + "&json&c=" + g_user.lists[f].updated.replace(/[^\d]/g, "")
                  });
                  this.profilerMenu.push([g_user.lists[f].id, g_user.lists[f].name, this.SetProfile.bind(this, g_user.lists[f].id)])
               }
            }
         }
      }
      var g = $WH.ce("span");
      g.style.paddingLeft = "12px";
      $WH.ae(this.sideDiv, g);
      $WH.ae(g, document.createTextNode(LANG.missioncalc_myfollowers));
      if (this.profiler) {
         g.menu = this.profilerMenu;
         Menu.add(g, g.menu, {
            showAtElement: true
         });
         (Menu.findItem(this.profilerMenu, [0])).checked = true
      } else {
         var d = function() {
            var j = 0;
            for (var i in this.followers) {
               if (this.followers[i].hasOwnProperty("follower") && this.followers[i].follower.id) {
                  j++
               }
            }
            if (j > 0) {
               if (!confirm(LANG.missioncalc_leavecheck)) {
                  return
               }
            }
            location.href = "/client"
         };
         $WH.aE(g, "click", d.bind(this));
         $WH.Tooltip.simple(g, LANG.missioncalc_clientprofilertip)
      }
      var b = $WH.ce("span");
      b.style.paddingLeft = "12px";
      $WH.ae(this.sideDiv, b);
      $WH.ae(b, document.createTextNode(LANG.missioncalc_autofill));
      $WH.Tooltip.simple(b, LANG.missioncalc_autofilltip);
      $WH.aE(b, "click", (function(i) {
         return function() {
            if (i.profiler && i.currentProfile) {
               i.AutoFill.call(i)
            } else {
               return alert(LANG.missioncalc_selectprofilefirsttip)
            }
         }
      })(this));
      this.successDiv = $WH.ce("div");
      this.successDiv.className = "success";
      $WH.ae(this.sideDiv, this.successDiv);
      $WH.ae(this.successDiv, document.createTextNode(LANG.missioncalc_successchance + LANG.colon));
      this.successSpan = $WH.ce("span");
      this.successSpan.className = "success";
      $WH.ae(this.successDiv, this.successSpan);
      this.followers = {};
      for (var a = 1; a <= this.mission.followers; a++) {
         this.CreateFollower(a);
         $WH.ae(this.div, this.followers[a].div)
      }
      $WH.st(this.successSpan, "" + this.CalculateSuccessChance().chance + "%");
      if (this.callback != null) {
         this.callback()
      }
   },
   SetSide: function(b) {
      this.side = b;
      this.sideDiv.setAttribute("data-side", this.side);
      for (var a in this.followers) {
         if (this.followers[a].follower.hasOwnProperty("id")) {
            this.UpdateFollowerDisplay(a)
         }
      }
   },
   SetProfile: function(c) {
      (Menu.findItem(this.profilerMenu, [this.currentProfile])).checked = false;
      this.currentProfile = c;
      (Menu.findItem(this.profilerMenu, [this.currentProfile])).checked = true;
      for (var a = 0, b; b = g_user.lists[a]; a++) {
         if (b.id != c) {
            continue
         }
         if (this.side != g_file_factions[g_file_racefactions[b.race]]) {
            this.SetSide(g_file_factions[g_file_racefactions[b.race]])
         }
         break
      }
   },
   CreateFollower: function(b) {
      var c;
      if (!this.followers.hasOwnProperty(b)) {
         c = {
            follower: {}
         };
         c.div = $WH.ce("div")
      } else {
         c = this.followers[b];
         $WH.ee(c.div)
      }
      c.div.className = "follower follower" + b + " empty";
      c.nameDiv = $WH.ce("div");
      c.nameDiv.className = "name";
      $WH.ae(c.div, c.nameDiv);
      $WH.st(c.nameDiv, LANG.missioncalc_chooseafollower);
      $WH.aE(c.nameDiv, "click", this.ChooseFollower.bind(this, b));
      c.avatarDiv = $WH.ce("div");
      c.avatarDiv.className = "no-empty avatar";
      $WH.ae(c.div, c.avatarDiv);
      $WH.aE(c.avatarDiv, "click", this.ChooseFollower.bind(this, b));
      $WH.Tooltip.simple(c.avatarDiv, LANG.missioncalc_clicktochange, "q2");
      c.avatarPortrait = $WH.ce("div");
      c.avatarPortrait.className = "no-empty avatar-portrait";
      $WH.ae(c.avatarDiv, c.avatarPortrait);
      c.avatarLevel = $WH.ce("div");
      c.avatarLevel.className = "no-empty avatar-level";
      c.avatarLevel.innerHTML = "";
      $WH.ae(c.avatarDiv, c.avatarLevel);
      c.abilitiesDiv = $WH.ce("div");
      c.abilitiesDiv.className = "no-empty abilities";
      $WH.ae(c.div, c.abilitiesDiv);
      c.controlsDiv = $WH.ce("div");
      c.controlsDiv.className = "no-empty controls";
      $WH.ae(c.div, c.controlsDiv);
      c.qualitySelect = $WH.ce("div");
      c.qualitySelect.className = "quality";
      $WH.ae(c.controlsDiv, c.qualitySelect);
      c.levelSelect = $WH.ce("div");
      c.levelSelect.className = "follower-level";
      c.levelSelect.onmousedown = $WH.rf;
      c.levelSelect.onclick = function(f) {
         f.stopPropagation();
         return false
      };
      $WH.ae(c.div, c.levelSelect);
      c.itemLevelSelect = $WH.ce("div");
      c.itemLevelSelect.className = "follower-itemlevel";
      c.itemLevelSelect.onclick = function(f) {
         f.stopPropagation();
         return false
      };
      $WH.ae(c.div, c.itemLevelSelect);
      var d = this.minItemLevel[c.follower.quality] || this.minItemLevel["default"];
      var a = $WH.isset("g_ptr") && g_ptr ? this.maxItemLevel.ptr : this.maxItemLevel["default"];
      c.itemLevelSlider = Slider.init(c.itemLevelSelect, {
         minValue: d,
         maxValue: a,
         trackSize: 154,
         handleSize: 9,
         onMove: this.UpdateFollowerItemLevel.bind(this, b)
      });
      Slider.setValue(c.itemLevelSlider, c.chosenItemLevel || 600);
      c.itemLevelSlider.onmouseover = function(f) {
         $WH.Tooltip.showAtCursor(f, LANG.tooltip_changeitemlevel2, 0, 0, "q2")
      };
      c.itemLevelSlider.onmousemove = $WH.Tooltip.cursorUpdate;
      c.itemLevelSlider.onmouseout = $WH.Tooltip.hide;
      c.itemLevelSlider.input.onmouseover = function(f) {
         $WH.Tooltip.showAtCursor(f, LANG.tooltip_changeitemlevel, 0, 0, "q2")
      };
      c.itemLevelSlider.input.onmousemove = $WH.Tooltip.cursorUpdate;
      c.itemLevelSlider.input.onmouseout = $WH.Tooltip.hide;
      c.itemLevelEnabled = false;
      this.followers[b] = c;
      if (!this.updatingDisplay) {
         this.UpdateFollowerChosenAttributes(b)
      }
   },
   CheckToDisableItemLevel: function(a) {
      var b = this.followers[a];
      if (b.levelSelect) {
         if (b.chosenLevel != this.maxLevel) {
            b.itemLevelEnabled = false;
            b.itemLevelSelect.style.display = "none"
         } else {
            b.itemLevelEnabled = true;
            b.itemLevelSelect.style.display = "block"
         }
         this.UpdateFollowerItemLevel(a)
      }
   },
   UpdateFollowerChosenAttributes: function(a) {
      this.UpdateFollowerQuality(a);
      this.UpdateFollowerLevel(a);
      this.UpdateFollowerItemLevel(a)
   },
   UpdateFollowerQuality: function(b, a) {
      var c = this.followers[b];
      c.chosenQuality = a || c.div.getAttribute("data-quality");
      this.UpdateFollowerDisplay(b);
      $WH.st(this.successSpan, "" + this.CalculateSuccessChance().chance + "%")
   },
   UpdateFollowerLevel: function(b, a) {
      var c = this.followers[b];
      c.chosenLevel = a || c.div.getAttribute("data-level");
      this.UpdateFollowerDisplay(b);
      this.CheckToDisableItemLevel(b);
      $WH.st(this.successSpan, "" + this.CalculateSuccessChance().chance + "%")
   },
   UpdateFollowerItemLevel: function(c, d, b, a) {
      var e = this.followers[c];
      if (!e.chosenItemLevel) {
         if (e.itemLevelSlider && e.itemLevelSlider._min) {
            e.chosenItemLevel = e.itemLevelSlider._min
         } else {
            e.chosenItemLevel = 600
         }
      }
      if (a && a.value) {
         e.chosenItemLevel = a.value
      }
      $WH.st(this.successSpan, "" + this.CalculateSuccessChance().chance + "%")
   },
   ChooseFollower: function(a) {
      this.choosingSlot = a;
      Lightbox.show("follower", {
         onShow: this.picker.onFollowerPickerShow.bind(this)
      })
   },
   UpdateFollowerDisplay: function(c) {
      if (this.updatingDisplay) {
         return
      }
      this.updatingDisplay = true;
      var j = this.followers[c];
      this.CreateFollower(c);
      if (!j.follower.hasOwnProperty("id")) {
         this.updatingDisplay = false;
         return
      }
      j.div.setAttribute("data-class", j.follower[this.side].baseclass);
      j.avatarPortrait.style.backgroundImage = 'url("' + g_staticUrl + "/images/wow/garr/" + j.follower[this.side].portrait + '.png")';
      var k = $WH.ce("a");
      k.href = "/follower=" + j.follower.id + "." + (this.side == "alliance" ? "1" : "2");
      k.rel = [j.chosenQuality ? "q=" + j.chosenQuality : false, j.chosenLevel ? "level=" + j.chosenLevel : false, (j.chosenAbilities && j.chosenAbilities.length) ? "abil=" + j.chosenAbilities.join(":") : false].filter(function(f) {
         return !!f
      }).join("&");
      k.className = "q";
      $WH.st(k, j.follower[this.side].name);
      $WH.aE(k, "click", function(a) {
         if (a.button == 0) {
            a.preventDefault()
         }
      });
      $WH.ee(j.nameDiv);
      $WH.ae(j.nameDiv, k);
      $WH.Tooltip.simple(j.nameDiv, LANG.missioncalc_clicktochange, "q2");
      $WH.ee(j.abilitiesDiv);
      var g = function(p, q) {
         if (!g_garrison_abilities.hasOwnProperty(p)) {
            return
         }
         var o = g_garrison_abilities[p];
         var a = Icon.create(o.icon, 0, null, "javascript:;");
         a.className += " garrison-ability";
         var f = a.getElementsByTagName("a")[0];
         f.rel = "garrisonability=" + o.id;
         if (q >= 0) {
            $WH.aE(a, "click", this.ChooseAbility.bind(this, c, q));
            $WH.Tooltip.addTooltipText(f, LANG.missioncalc_clicktochange, "q2")
         }
         $WH.ae(j.abilitiesDiv, a)
      };
      var d = this.AllowedAbilityAddition(c, -1);
      if (j.chosenAbilities) {
         for (x in j.chosenAbilities) {
            g.call(this, j.chosenAbilities[x], x)
         }
      }
      if (d) {
         var e = $WH.ce("a");
         e.href = "javascript:;";
         e.className = "fa fa-plus";
         $WH.aE(e, "click", this.ChooseAbility.bind(this, c, -1));
         var n = "";
         switch (d) {
            case 1:
               n = g_garrison_ability_types[0];
               break;
            case 2:
               n = g_garrison_ability_types[1];
               break;
            default:
               n = g_garrison_ability_types[0] + "/" + g_garrison_ability_types[1];
               break
         }
         $WH.Tooltip.simple(e, LANG.add + " " + n, "q2", true);
         $WH.ae(j.abilitiesDiv, e)
      }
      $WH.ee(j.qualitySelect);
      if (j.follower.quality < this.qualityConstant.maximum) {
         var b = [];
         for (var m = this.qualityConstant.minimum; m <= this.qualityConstant.maximum; m++) {
            if (m >= j.follower.quality) {
               b.push([m, g_item_qualities[m], this.UpdateFollowerQuality.bind(this, c, m), null, {
                  className: "q" + m,
                  checkedFunc: function(a) {
                     return a[0] == j.chosenQuality
                  }
               }])
            }
         }
         j.qualitySelect.menu = b;
         Menu.add(j.qualitySelect, j.qualitySelect.menu)
      }
      if (!j.chosenQuality) {
         j.chosenQuality = j.follower.quality
      }
      j.div.setAttribute("data-quality", j.chosenQuality);
      j.qualitySelect.innerHTML = g_item_qualities[j.chosenQuality];
      j.qualitySelect.className = "quality q" + j.chosenQuality;
      if (j.qualitySelect.menu) {
         j.qualitySelect.className += " fa fa-caret-down fa-placement-spaced"
      }
      $WH.ee(j.levelSelect);
      if (j.follower.level < this.maxLevel) {
         var l = [];
         for (var h = 90; h <= this.maxLevel; h++) {
            if (h >= j.follower.level) {
               l.push([h, h, this.UpdateFollowerLevel.bind(this, c, h), null, {
                  checkedFunc: function(a) {
                     return a[0] == j.chosenLevel
                  }
               }])
            }
         }
         j.levelSelect.menu = l;
         Menu.add(j.levelSelect, j.levelSelect.menu)
      }
      if (!j.chosenLevel) {
         j.chosenLevel = j.follower.level
      }
      j.div.setAttribute("data-level", j.chosenLevel);
      j.levelSelect.innerHTML = j.chosenLevel;
      j.levelSelect.className = "follower-level level-" + j.chosenLevel;
      if (j.levelSelect.menu) {
         j.levelSelect.className += " fa fa-caret-down fa-placement-spaced"
      }
      this.UpdateFollowerChosenAttributes(c);
      j.div.className = j.div.className.replace(" empty", "");
      this.UpdateMechanicCounters();
      $WH.st(this.successSpan, "" + this.CalculateSuccessChance().chance + "%");
      this.updatingDisplay = false
   },
   ChooseAbility: function(c, b) {
      this.choosingSlot = c;
      var a = -1;
      if (b >= 0) {
         a = this.followers[c].chosenAbilities[b]
      }
      this.choosingAbility = {
         index: b,
         allowed: this.AllowedAbilityAddition(c, a)
      };
      Lightbox.show("ability", {
         onShow: this.picker.onAbilityPickerShow.bind(this)
      })
   },
   AllowedAbilityAddition: function(b, a) {
      var e = this.followers[b];
      var h = parseInt(e.chosenQuality || e.follower.quality, 10);
      var c = {
         abilities: 0,
         traits: 0,
         needMore: 0
      };
      var g = function(i) {
         if (!g_garrison_abilities.hasOwnProperty(i)) {
            return
         }
         if (i == a) {
            return
         }
         var f = g_garrison_abilities[i];
         c[f.trait ? "traits" : "abilities"]++
      };
      if (e.chosenAbilities) {
         for (x in e.chosenAbilities) {
            g(e.chosenAbilities[x])
         }
      }
      var d = function(j, i) {
         if (i <= 0) {
            return
         }
         if ((!e.chosenAbilities) || (!e.chosenAbilities.length)) {
            return
         }
         for (var f = 0; f < e.chosenAbilities.length; f++) {
            var k = e.chosenAbilities[f];
            if (g_garrison_abilities[k].trait == ((j & 2) > 0)) {
               e.chosenAbilities.splice(f--, 1);
               c[g_garrison_abilities[k].trait ? "traits" : "abilities"]--;
               if (--i <= 0) {
                  break
               }
            }
         }
      };
      switch (h) {
         case 2:
            d(1, c.abilities - 1);
            d(2, c.traits - 1);
            c.needMore += (c.abilities < 1 ? 1 : 0) + (c.traits < 1 ? 2 : 0);
            break;
         case 3:
            d(1, c.abilities - 2);
            d(2, c.traits - 2);
            d(3, c.abilities + c.traits - 3);
            if (c.abilities + c.traits >= 3) {
               c.needMore = 0
            } else {
               if (c.abilities >= 2) {
                  c.needMore = 2
               } else {
                  if (c.traits >= 2) {
                     c.needMore = 1
                  } else {
                     c.needMore = 3
                  }
               }
            }
            break;
         case 4:
            d(1, c.abilities - 2);
            d(2, c.traits - 3);
            c.needMore += (c.abilities < 2 ? 1 : 0) + (c.traits < 3 ? 2 : 0);
            break;
         case 5:
            d(1, c.abilities - 3);
            d(2, c.traits - 3);
            c.needMore += (c.abilities < 3 ? 1 : 0) + (c.traits < 3 ? 2 : 0);
            break
      }
      return c.needMore
   },
   UpdateMechanicCounters: function() {
      var e = {};
      var c, b;
      for (var a = 1; a <= this.mission.followers; a++) {
         if (this.followers.hasOwnProperty(a) && this.followers[a].follower.hasOwnProperty("id")) {
            c = this.followers[a].follower;
            if (this.followers[a].chosenAbilities) {
               for (var g = 0; g < this.followers[a].chosenAbilities.length; g++) {
                  if (g_garrison_abilities.hasOwnProperty(this.followers[a].chosenAbilities[g])) {
                     b = g_garrison_abilities[this.followers[a].chosenAbilities[g]];
                     for (var d = 0; d < b.counters.length; d++) {
                        if (b.counters[d]) {
                           if (!e.hasOwnProperty(b.counters[d])) {
                              e[b.counters[d]] = 0
                           }
                           e[b.counters[d]]++
                        }
                     }
                  }
               }
            }
         }
      }
      $(".garrison-encounter-enemy .iconmedium").each(function(h) {
         var i = $(this);
         var f;
         if (i.attr("data-mechanic")) {
            f = i.attr("data-mechanic");
            if (e.hasOwnProperty(f)) {
               i.addClass("garrison-ability-countered");
               if (--e[f] <= 0) {
                  delete e[f]
               }
            } else {
               i.removeClass("garrison-ability-countered")
            }
         }
      });
      $("table.infobox a").each(function(h) {
         var k = $(this);
         var i = /\/garrisonabilities\?filter=cr=2;crs=(\d+);crv=0$/;
         var j;
         if (j = i.exec(this.href)) {
            var f = j[1];
            k.addClass("mechanic");
            if (e.hasOwnProperty(f)) {
               k.addClass("countered");
               if (--e[f] <= 0) {
                  delete e[f]
               }
            } else {
               k.removeClass("countered")
            }
         }
      })
   },
   GetFollowerBias: function(d, c) {
      var a = (d - this.mission.level) * $WH.fround(1 / 3);
      if (this.mission.level == this.maxLevel && this.mission.itemlevel > 0) {
         a += (c - this.mission.itemlevel) * $WH.fround(1 / 15)
      }
      var b = -1;
      if (a < -1 || (b = 1, a > 1)) {
         a = b
      }
      return a
   },
   ThreatCounterIsAlreadyRegistered: function(a, b) {
      return this.registeredThreatCounters.hasOwnProperty(a) && this.registeredThreatCounters[a].hasOwnProperty(b) && this.registeredThreatCounters[a][b]
   },
   RegisterThreatCounter: function(a, b, c) {
      if (!this.registeredThreatCounters.hasOwnProperty(a)) {
         this.registeredThreatCounters[a] = {}
      }
      if (!this.registeredThreatCounters[a].hasOwnProperty(b)) {
         this.registeredThreatCounters[a][b] = {}
      }
      this.registeredThreatCounters[a][b][c] = true
   },
   CalcChance: function(b, a, c) {
      var d;
      if (c >= 0) {
         d = (a - b) * c + b
      } else {
         d = (c + 1) * b
      }
      return d
   },
   CheckEffectRace: function(c, a, d) {
      if (this.mission.followers > 0) {
         for (var e = 0; e < c.length; ++e) {
            if (e == d) {
               continue
            }
            var f = c[e];
            var b = g_garrison_followers && g_garrison_followers[f.follower] && g_garrison_followers[f.follower][this.side] ? g_garrison_followers[f.follower][this.side] : null;
            if (b == null) {
               continue
            }
            if (b.race == a) {
               return true
            }
         }
      }
      return false
   },
   GetMissionTimes: function(g) {
      var e = this.mission.missiontime;
      var h = this.mission.traveltime;
      for (var c = 0; c < g.length; ++c) {
         var d = g[c];
         for (var b = 0; b < d.abilities.length; ++b) {
            var a = g_garrison_abilities[d.abilities[b]];
            var f = a.type.length;
            for (var k = 0; k < f; ++k) {
               if (a.type[k] == 3) {
                  h *= a.amount4[k]
               }
               if (a.type[k] == 17) {
                  e *= a.amount4[k]
               }
            }
         }
      }
      return {
         missiontime: Math.floor(e),
         traveltime: Math.floor(h)
      }
   },
   GetMentorInfo: function(h) {
      var f = 0;
      var a = 0;
      if (this.mission.followers > 0) {
         for (var d = 0; d < h.length; ++d) {
            var e = h[d];
            for (var c = 0; c < e.abilities.length; ++c) {
               var b = g_garrison_abilities[e.abilities[c]];
               var g = b.type.length;
               for (var k = 0; k < g; ++k) {
                  if (b.type[k] == 18) {
                     if (e.level > f) {
                        f = e.level
                     }
                     if (e.avgilvl > a) {
                        a = e.avgilvl
                     }
                  }
               }
            }
         }
      }
      return {
         level: f,
         itemlevel: a
      }
   },
   AutoFill: function() {
      if (!this.profiler[this.currentProfile] || !this.mission.followers) {
         return
      }
      var e = [];
      for (var d in this.profiler[this.currentProfile]) {
         if (this.profiler[this.currentProfile].hasOwnProperty(d) && this.profiler[this.currentProfile][d].active == 1) {
            e.push(parseInt(d))
         }
      }
      if (!e.length) {
         return
      }
      var b = [];
      var g = {};
      var C = false;
      if (this.mission && this.mission.encounters) {
         for (var y in this.mission.encounters) {
            if (this.mission.encounters[y].mechanics) {
               for (var v in this.mission.encounters[y].mechanics) {
                  var f = this.mission.encounters[y].mechanics[v].type;
                  if (b.indexOf(f) == -1) {
                     b.push(f)
                  } else {
                     g[f] = true;
                     C = true
                  }
               }
            }
         }
      }
      var z = this.GetCombinations(e, this.mission.followers);
      if (this.DEBUG) {
         console.log("Total combinations: %d", z.length)
      }
      var a = [];
      var r, m, h, B, c;
      for (y = 0; y < z.length; ++y) {
         r = false;
         m = {};
         if (this.mission.followers <= 3 && C) {
            loops: for (v = 0; v < b.length; ++v) {
               var s = b[v];
               if (!m[s]) {
                  m[s] = 0
               }
               for (var u = 0; u < z[y].length; ++u) {
                  h = this.profiler[this.currentProfile][z[y][u]];
                  for (var t = 0; t < h.abilities.length; ++t) {
                     B = g_garrison_abilities[h.abilities[t]];
                     c = B.type.length;
                     for (var A = 0; A < c; ++A) {
                        if (s == B.counters[A]) {
                           ++m[s];
                           if (m[s] > 1 && g[s]) {
                              r = true;
                              break loops
                           }
                        }
                     }
                  }
               }
            }
         }
         if (r) {
            a = a.concat(this.GetPermutations(z[y]))
         } else {
            a = a.concat([z[y]])
         }
      }
      if (this.DEBUG) {
         console.log("Total combinations+permutations: %d", a.length)
      }
      var w = [];
      var p;
      for (y = 0; y < a.length; ++y) {
         p = [];
         for (v = 0; v < a[y].length; ++v) {
            p.push(this.profiler[this.currentProfile][a[y][v]])
         }
         w.push({
            chanceInfo: this.CalculateSuccessChance.call(this, p),
            combo: a[y]
         })
      }
      w.sort(function(j, i) {
         return i.chanceInfo.chance - j.chanceInfo.chance
      });
      var n = [];
      for (y = 0; y < w.length; ++y) {
         if (w[y].chanceInfo.chance == w[0].chanceInfo.chance) {
            n.push(w[y])
         }
      }
      var o = this;
      n.sort(function(j, i) {
         j.xp = o.GetTotalXPGain.call(o, j);
         i.xp = o.GetTotalXPGain.call(o, i);
         return i.xp - j.xp
      });
      var q = [];
      for (y = 0; y < n.length; ++y) {
         if (!("xp" in n[y]) || (n[y].xp == n[0].xp)) {
            q.push(n[y])
         }
      }
      q.sort(function(H, F) {
         var l = 0;
         var D = 0;
         var I = 0;
         var j = 0;
         for (var k = 0; k < H.combo.length; ++k) {
            var G = o.profiler[o.currentProfile][H.combo[k]];
            l += G.level;
            I += G.quality;
            var E = o.profiler[o.currentProfile][F.combo[k]];
            D += E.level;
            j += E.quality
         }
         if (l != D) {
            return l - D
         }
         return I - j
      });
      for (y = 0; y < q[0].combo.length; ++y) {
         this.choosingSlot = parseInt(y) + 1;
         h = g_garrison_followers && g_garrison_followers[q[0].combo[y]] ? g_garrison_followers[q[0].combo[y]] : null;
         if (h != null) {
            this.picker.chosenFollower.call(this, h)
         }
      }
   },
   GetTotalXPGain: function(g) {
      var m = g.combo;
      var q = g.chanceInfo.chanceOver / 100;
      var c = this.mission.experience;
      var h = 0;
      for (var f in this.mission.rewards) {
         if (f == "experience") {
            for (var e = 0; e < this.mission.rewards[f].length; ++e) {
               h += this.mission.rewards[f][e]
            }
         }
      }
      var p = 0;
      for (f = 0; f < m.length; ++f) {
         var l = this.profiler[this.currentProfile][m[f]];
         if (l.level >= this.maxLevel && l.quality >= 4) {
            continue
         }
         var o = 1;
         if (this.mission.level) {
            if (l.level <= (this.mission.level - 3)) {
               o = 0.1
            } else {
               if (l.level < this.mission.level) {
                  o = 0.5
               }
            }
         }
         if (this.mission.itemlevel) {
            if (l.avgilvl <= (this.mission.itemlevel - 11)) {
               o = 0.1
            } else {
               if (l.avgilvl < this.mission.itemlevel) {
                  o = 0.5
               }
            }
         }
         var r = 0;
         for (e = 0; e < m.length; ++e) {
            var b = this.profiler[this.currentProfile][m[e]];
            for (var d = 0; d < b.abilities.length; ++d) {
               var a = g_garrison_abilities[b.abilities[d]];
               var n = a.type.length;
               for (var s = 0; s < n; ++s) {
                  if (a.type[s] == 4) {
                     switch (a.missionparty[s]) {
                        case 2:
                           r += 1 - a.amount4[s];
                           break;
                        case 1:
                           if (f == e) {
                              r += 1 - a.amount4[s]
                           }
                           break;
                        default:
                           break
                     }
                  }
               }
            }
         }
         p += (c + (q + r) * c) * o;
         p += (h + h * r) * o
      }
      return p
   },
   GetPermutations: function(a) {
      var b = [];
      if (a.length === 1) {
         return [a]
      }
      for (var e = 0; e < a.length; e++) {
         var d = this.GetPermutations.call(this, a.slice(0, e).concat(a.slice(e + 1)));
         for (var c = 0; c < d.length; c++) {
            d[c].unshift(a[e]);
            b.push(d[c])
         }
      }
      return b
   },
   GetCombinations: function(a, b) {
      var f, c, e, d, g;
      if (b > a.length || b <= 0) {
         return []
      }
      if (b == a.length) {
         return [a]
      }
      if (b == 1) {
         e = [];
         for (f = 0; f < a.length; f++) {
            e.push([a[f]])
         }
         return e
      }
      e = [];
      for (f = 0; f < a.length - b + 1; f++) {
         d = a.slice(f, f + 1);
         g = this.GetCombinations.call(this, a.slice(f + 1), b - 1);
         for (c = 0; c < g.length; c++) {
            e.push(d.concat(g[c]))
         }
      }
      return e
   },
   CalculateSuccessChance: function(c) {
      var n = (this.DEBUG && !c) ? true : false;
      if (n) {
         console.log("----- Start ComputeSuccessChance -----")
      }
      var s = [];
      if (c) {
         s = c
      } else {
         for (var w in this.followers) {
            if (this.followers[w].hasOwnProperty("follower") && this.followers[w].follower.id) {
               s.push({
                  follower: this.followers[w].follower.id,
                  abilities: this.followers[w].chosenAbilities ? this.followers[w].chosenAbilities.slice() : [],
                  avgilvl: this.followers[w].chosenItemLevel ? parseInt(this.followers[w].chosenItemLevel) : this.followers[w].follower.itemlevel,
                  level: this.followers[w].chosenLevel ? parseInt(this.followers[w].chosenLevel) : this.followers[w].follower.level,
                  quality: this.followers[w].chosenQuality ? parseInt(this.followers[w].chosenQuality) : this.followers[w].follower.quality
            })
         }
      }
   }
   var d = [];
if (this.mission && this.mission.encounters) {
   for (w in this.mission.encounters) {
      if (this.mission.encounters[w].mechanics) {
         for (var v in this.mission.encounters[w].mechanics) {
            d.push(this.mission.encounters[w].mechanics[v])
         }
      }
   }
}
var o = this.GetMentorInfo(s);
var r;
for (w = 0; w < s.length; ++w) {
   r = s[w];
   if (o.level > r.level) {
      if (n) {
         console.log("Mentored follower %d from level %d to level %d.", r.follower, r.level, o.level)
      }
      r.level = o.level
   }
   if (o.itemlevel > r.avgilvl) {
      if (n) {
         console.log("Mentored follower %d from item level %d to item level %d.", r.follower, r.avgilvl, o.itemlevel)
      }
      r.avgilvl = o.itemlevel
   }
   r.bias = $WH.fround(this.GetFollowerBias(r.level, r.avgilvl));
   if (n) {
      console.log("Follower %d bias: %.2f", r.follower, r.bias)
   }
}
var D = this.mission.followers * 100;
var B = D;
if (d.length > 0) {
   for (w = 0; w < d.length; ++w) {
      var C = d[w];
      if (C.category != 2) {
         D = B
      } else {
         D = B + C.amount;
         B += C.amount
      }
   }
}
if (D <= 0) {
   return {
      chance: 100,
      chanceOver: 0
   }
}
var b = $WH.fround(100 / D);
if (n) {
   console.log("coeff: ", b)
}
var l = 0;
for (w = 0; w < s.length; ++w) {
   r = s[w];
   var z = $WH.fround(this.CalcChance(100, 150, r.bias) * b);
   l += z;
   if (n) {
      console.log("Added %.2f to success due to follower %d bias.", z, r.follower)
   }
}
var A = 0;
var f = 0;
this.registeredThreatCounters = {};
if (d.length > 0) {
   do {
      C = d[A];
      if (!C.category || C.category == 2) {
         var m = C.amount;
         if (this.mission.followers > 0) {
            for (w = 0; w < s.length; ++w) {
               r = s[w];
               for (v = 0; v < r.abilities.length; ++v) {
                  var F = g_garrison_abilities[r.abilities[v]];
                  var e = F.type.length;
                  for (var E = 0; E < e; ++E) {
                     if (C.type == F.counters[E] && !(F.amount1[E] & 1) && m > 0 && !this.ThreatCounterIsAlreadyRegistered(w, v, E)) {
                        var q = this.CalcChance(F.amount2[E], F.amount3[E], r.bias);
                        var a = C.amount;
                        if (q <= a) {
                           a = q
                        }
                        this.RegisterThreatCounter(w, v, E);
                        m = m - a
                     }
                  }
               }
            }
         }
         if (m < 0) {
            m = 0
         }
         f = $WH.fround((C.amount - m) * b);
         l += f;
         if (n) {
            console.log("Added %.2f to success due to followers countering boss mechanic %d.", f, C.id)
         }
      }++A
   } while (A < d.length)
}
for (A = 0; A < d.length; ++A) {
   C = d[A];
   if (C.category == 1) {
      if (this.mission.followers > 0) {
         for (w = 0; w < s.length; ++w) {
            r = s[w];
            for (v = 0; v < r.abilities.length; ++v) {
               F = g_garrison_abilities[r.abilities[v]];
               e = F.type.length;
               for (E = 0; E < e; ++E) {
                  if (C.type == F.counters[E]) {
                     q = this.CalcChance(F.amount2[E], F.amount3[E], r.bias);
                     q *= b;
                     q = $WH.fround(q);
                     l += q;
                     if (n) {
                        console.log("Added %.2f to success due to follower %d enemy race ability %d.", q, r.follower, C.id)
                     }
                  }
               }
            }
         }
      }
   }
}
if (this.mission.followers > 0) {
   for (w = 0; w < s.length; ++w) {
      r = s[w];
      for (v = 0; v < r.abilities.length; ++v) {
         F = g_garrison_abilities[r.abilities[v]];
         e = F.type.length;
         for (E = 0; E < e; ++E) {
            if (F.counters[E] && F.counters[E] == this.mission.mechanictype) {
               q = this.CalcChance(F.amount2[E], F.amount3[E], r.bias);
               q *= b;
               q = $WH.fround(q);
               l += q;
               if (n) {
                  console.log("Added %.2f to success due to follower %d environment ability %d.", q, r.follower, F.id)
               }
            }
         }
      }
   }
}
var y = this.GetMissionTimes(s);
if (this.mission.followers > 0) {
   for (w = 0; w < s.length; ++w) {
      r = s[w];
      for (v = 0; v < r.abilities.length; ++v) {
         F = g_garrison_abilities[r.abilities[v]];
         e = F.type.length;
         for (E = 0; E < e; ++E) {
            var u = false;
            switch (F.type[E]) {
               case 1:
                  if (s.length == 1) {
                     u = true
                  }
                  break;
               case 2:
                  u = true;
                  break;
               case 5:
                  if (this.CheckEffectRace.call(this, s, F.race[E], w)) {
                     u = true
                  }
                  break;
               case 6:
                  if (y.missiontime > 3600 * F.hours[E]) {
                     u = true
                  }
                  break;
               case 7:
                  if (y.missiontime < 3600 * F.hours[E]) {
                     u = true
                  }
                  break;
               case 9:
                  if (y.traveltime > 3600 * F.hours[E]) {
                     u = true
                  }
                  break;
               case 10:
                  if (y.traveltime < 3600 * F.hours[E]) {
                     u = true
                  }
                  break;
               default:
                  break
            }
            if (u) {
               q = this.CalcChance(F.amount2[E], F.amount3[E], r.bias);
               q *= b;
               q = $WH.fround(q);
               l += q;
               if (n) {
                  console.log("Added %.2f to success due to follower %d trait %d.", q, r.follower, F.type[E])
               }
            }
         }
      }
   }
}
if (n) {
   console.log("Total before adding base chance: %.2f", l)
}
var t = true;
var k = 100;
var h;
var p = (((100 - this.mission.basebonuschance) * l) * 0.01) + this.mission.basebonuschance;
if (n) {
   console.log("Total after base chance: %.2f", p)
}
h = p;
var g = h;
if (t && k <= p) {
   h = k
}
if (n) {
   if (t && g > 100) {
      console.log("Total success chance: %.2f, (%.2f before clamping).", h, g)
   } else {
      console.log("Total success chance: %.2f.", h)
   }
   console.log("----- End ComputeSuccessChance -----")
}
return {
   chance: Math.floor(h),
   chanceOver: g - h
}
},
picker: {
   curSide: "",
       onFollowerPickerShow: function(k, g, b) {
      Lightbox.setSize(800, 564);
      var e;
      var l = function(n) {
         var m = {};
         for (var a in this.followers) {
            if (this.followers.hasOwnProperty(a) && this.followers[a].follower.hasOwnProperty("id")) {
               m[this.followers[a].follower.id] = 1
            }
         }
         var d = m.hasOwnProperty(n.id);
         if (this.currentProfile) {
            d |= !this.profiler[this.currentProfile].hasOwnProperty(n.id)
         }
         return !d
      };
      if (g) {
         this.picker.curSide = this.side;
         k.className = "listview";
         k.style.borderWidth = "10px";
         k.style.borderStyle = "solid";
         k.style.borderColor = "#404040";
         k.style.backgroundColor = "#404040";
         k.style.borderBottomWidth = "0";
         var h = $WH.ce("div"),
             j = $WH.ce("a"),
             f = $WH.ce("div");
         h.className = "listview";
         h.style.borderWidth = "0";
         $WH.ae(k, h);
         j = $WH.ce("a");
         j.className = "dialog-x";
         j.href = "javascript:;";
         j.onclick = Lightbox.hide;
         $WH.ae(j, $WH.ct(LANG.close));
         $WH.ae(k, j);
         f.className = "clear";
         $WH.ae(k, f);
         var c = [];
         for (var i in g_garrison_followers) {
            if (g_garrison_followers.hasOwnProperty(i)) {
               c.push(g_garrison_followers[i])
            }
         }
         e = new Listview({
            template: "followerPicker",
            _calc: this,
            id: "followers",
            parent: h,
            clip: {
               h: 451
            },
            note: "",
            data: c,
            customFilter: l.bind(this)
         });
         if ($WH.Browser.firefox) {
            $WH.aE(e.getClipDiv(), "DOMMouseScroll", g_pickerWheel)
         } else {
            e.getClipDiv().onmousewheel = g_pickerWheel
         }
      } else {
         e = g_listviews.followers;
         if (this.picker.curSide != this.side) {
            this.picker.curSide = this.side
         }
         e.clearSearch();
         e.updateFilters(true);
         e.indexCreated = false;
         e.applySort();
         e.refreshRows(true)
      }
      $WH.ee(e.thead);
      e.createHeader();
      e.updateSortArrow();
      setTimeout(function() {
         e.quickSearchBox.focus()
      }, 1)
   },
   chosenFollower: function(c) {
      Lightbox.hide();
      var b = this.followers[this.choosingSlot];
      b.follower = c;
      delete b.chosenLevel;
      delete b.chosenItemLevel;
      delete b.chosenQuality;
      delete b.chosenAbilities;
      if (this.currentProfile) {
         var a = this.profiler[this.currentProfile][c.id];
         b.chosenLevel = a.level;
         b.chosenItemLevel = a.avgilvl;
         b.chosenQuality = a.quality;
         b.chosenAbilities = a.abilities.slice()
      } else {
         if (c.hasOwnProperty(this.side) && c[this.side].hasOwnProperty("abilities")) {
            b.chosenAbilities = c[this.side].abilities.slice()
         }
      }
      this.UpdateFollowerDisplay(this.choosingSlot);
      this.choosingSlot = undefined
   },
   onAbilityPickerShow: function(l, g, b) {
      Lightbox.setSize(800, 564);
      var e;
      var k = function(o) {
         var m = {};
         var n = this.followers[this.choosingSlot];
         if (n.chosenAbilities) {
            for (var a = 0; a < n.chosenAbilities.length; a++) {
               if (a == this.choosingAbility.index) {
                  continue
               }
               m[n.chosenAbilities[a]] = 1
            }
         }
         var d = false;
         d = m.hasOwnProperty(o.id);
         d |= (this.choosingAbility.allowed & (o.trait ? 2 : 1)) == 0;
         d |= (o.side && o.side != this.side);
         d |= (o.followerclass.length && ($WH.in_array(o.followerclass, n.follower[this.side].classid) < 0));
         return !d
      };
      if (g) {
         this.picker.curSide = this.side;
         l.className = "listview";
         l.style.borderWidth = "10px";
         l.style.borderStyle = "solid";
         l.style.borderColor = "#404040";
         l.style.backgroundColor = "#404040";
         l.style.borderBottomWidth = "0";
         var h = $WH.ce("div"),
             j = $WH.ce("a"),
             f = $WH.ce("div");
         h.className = "listview";
         h.style.borderWidth = "0";
         $WH.ae(l, h);
         j = $WH.ce("a");
         j.className = "dialog-x";
         j.href = "javascript:;";
         j.onclick = Lightbox.hide;
         $WH.ae(j, $WH.ct(LANG.close));
         $WH.ae(l, j);
         f.className = "clear";
         $WH.ae(l, f);
         var c = [];
         for (var i in g_garrison_abilities) {
            if (g_garrison_abilities.hasOwnProperty(i)) {
               c.push(g_garrison_abilities[i])
            }
         }
         e = new Listview({
            template: "abilityPicker",
            _calc: this,
            id: "abilities",
            parent: h,
            clip: {
               h: 451
            },
            note: "",
            data: c,
            customFilter: k.bind(this)
         });
         if ($WH.Browser.firefox) {
            $WH.aE(e.getClipDiv(), "DOMMouseScroll", g_pickerWheel)
         } else {
            e.getClipDiv().onmousewheel = g_pickerWheel
         }
      } else {
         e = g_listviews.abilities;
         if (this.picker.curSide != this.side) {
            this.picker.curSide = this.side
         }
         e.clearSearch();
         e.updateFilters(true);
         e.refreshRows(true)
      }
      $WH.ee(e.thead);
      e.createHeader();
      e.updateSortArrow();
      setTimeout(function() {
         e.quickSearchBox.focus()
      }, 1)
   },
   chosenAbility: function(b) {
      Lightbox.hide();
      var a = this.followers[this.choosingSlot];
      if (this.choosingAbility.index >= 0) {
         a.chosenAbilities[this.choosingAbility.index] = b.id
      } else {
         if (!a.chosenAbilities) {
            a.chosenAbilities = []
         }
         a.chosenAbilities.push(b.id)
      }
      this.UpdateFollowerDisplay(this.choosingSlot);
      this.choosingSlot = undefined;
      this.choosingAbility = undefined
   }
}
};
jQuery.extend(Listview.templates, {
   followerPicker: {
      sort: [1],
      nItemsPerPage: -1,
      hideBands: 2,
      hideNav: 1 | 2,
      searchable: 1,
      searchDelay: 100,
      poundable: 0,
      filtrable: 0,
      clip: {
         w: 800,
         h: 486
      },
      columns: [{
         id: "name",
         name: LANG.name,
         type: "text",
         align: "left",
         compute: function(e, c, g) {
            var i = this._calc.currentProfile ? this._calc.profiler[this._calc.currentProfile][e.id] : e;
            var j = i.quality;
            var h = $WH.ce("a");
            h.className = "q" + j + " listview-cleartext";
            h.href = "/follower=" + e.id + "." + (this._calc.side == "alliance" ? 1 : 2);
            if (this._calc.currentProfile) {
               h.rel = "q=" + j + "&level=" + i.level + "&abil=" + i.abilities.join(":")
            }
            c.style.paddingLeft = "2.5em";
            if (e[this._calc.side].portrait) {
               c.style.backgroundImage = 'url("' + g_staticUrl + "/images/wow/garr/" + e[this._calc.side].portrait + '.png")';
               c.style.backgroundRepeat = "no-repeat";
               c.style.backgroundSize = "3em 3em";
               c.style.backgroundPosition = "-0.75em -0.25em"
            }
            $WH.ae(h, $WH.ct(e[this._calc.side].name));
            var b = $WH.ce("div");
            $WH.ae(c, b);
            $WH.ae(b, h);
            if (this._calc.currentProfile && !this._calc.profiler[this._calc.currentProfile][e.id].active) {
               b.style.position = "relative";
               var f = $WH.ce("div");
               f.className = "listview-name-info q10";
               $WH.ae(f, $WH.ct(LANG.inactive));
               $WH.aef(b, f)
            }
            var k = this;
            $(g).click(function(a) {
               if (a.which != 2 || a.target != h) {
                  a.preventDefault();
                  k._calc.picker.chosenFollower.call(k._calc, e)
               }
            })
         },
         getVisibleText: function(a) {
            return a[g_listviews.followers._calc.side].name
         },
         sortFunc: function(d, c) {
            return $WH.strcmp(d[g_listviews.followers._calc.side].name, c[g_listviews.followers._calc.side].name)
         }
      }, {
         id: "class",
         name: LANG.classs,
         type: "text",
         align: "left",
         width: "20%",
         compute: function(b, c, a) {
            $WH.st(c, b[this._calc.side].classs)
         },
         getVisibleText: function(a) {
            return a[g_listviews.followers._calc.side].classs
         },
         sortFunc: function(d, c) {
            return $WH.strcmp(d[g_listviews.followers._calc.side].classs, c[g_listviews.followers._calc.side].classs)
         }
      }, {
         id: "level",
         name: LANG.level,
         type: "number",
         compute: function(b, d) {
            var c = b.level;
            var a = Math.floor((b.itemlevelarmor + b.itemlevelweapon) / 2);
            if (this._calc.currentProfile) {
               c = this._calc.profiler[this._calc.currentProfile][b.id].level;
               a = this._calc.profiler[this._calc.currentProfile][b.id].avgilvl
            }
            $WH.st(d, "" + c + (c >= this._calc.maxLevel ? $WH.sprintf(LANG.qty, a) : ""))
         },
         sortFunc: function(d, c) {
            var i = d.level,
                g = c.level,
                f = Math.floor((d.itemlevelarmor + d.itemlevelweapon) / 2),
                e = Math.floor((c.itemlevelarmor + c.itemlevelweapon) / 2);
            if (this._calc.currentProfile) {
               var h = this._calc.profiler[this._calc.currentProfile];
               if (h[d.id]) {
                  i = h[d.id].level;
                  f = h[d.id].avgilvl
               }
               if (h[c.id]) {
                  g = h[c.id].level;
                  e = h[c.id].avgilvl
               }
            }
            return $WH.strcmp(i, g) || $WH.strcmp(f, e)
         }
      }, {
         id: "abilities",
         name: LANG.tab_abilities,
         compute: function(f, g, e) {
            g.style.padding = "0px";
            var c = this._calc.currentProfile ? this._calc.profiler[this._calc.currentProfile][f.id].abilities : f[this._calc.side].abilities;
            for (var a = 0; a < c.length; a++) {
               if (!g_garrison_abilities.hasOwnProperty(c[a])) {
                  continue
               }
               var d = g_garrison_abilities[c[a]];
               var b = Icon.create(d.icon, 0, null, "/garrisonability=" + d.id);
               b.className += " garrison-ability";
               b.style.display = "inline-block";
               $WH.ae(g, b)
            }
         },
         getVisibleText: function(e) {
            var c = "";
            var b = g_listviews.followers._calc.side;
            var d = g_listviews.followers._calc.currentProfile ? g_listviews.followers._calc.profiler[g_listviews.followers._calc.currentProfile][e.id].abilities : e[b].abilities;
            for (var a = 0; a < d.length; a++) {
               if (!g_garrison_abilities.hasOwnProperty(d[a])) {
                  continue
               }
               c += " " + g_garrison_abilities[d[a]].name
            }
            return c
         },
         sortFunc: function(d, c) {
            var e = d[this._calc.side].abilities;
            var f = c[this._calc.side].abilities;
            if (this._calc.currentProfile) {
               if (this._calc.profiler[this._calc.currentProfile][d.id]) {
                  e = this._calc.profiler[this._calc.currentProfile][d.id].abilities
               }
               if (this._calc.profiler[this._calc.currentProfile][c.id]) {
                  f = this._calc.profiler[this._calc.currentProfile][c.id].abilities
               }
            }
            return e.length - f.length
         }
      }]
   },
   abilityPicker: {
      sort: [1, 2],
      nItemsPerPage: -1,
      hideBands: 2,
      hideNav: 1 | 2,
      searchable: 1,
      searchDelay: 100,
      poundable: 0,
      filtrable: 0,
      clip: {
         w: 800,
         h: 486
      },
      columns: [{
         id: "name",
         name: LANG.name,
         type: "text",
         align: "left",
         value: "name",
         span: 2,
         compute: function(f, g, e) {
            var d = $WH.ce("td");
            d.style.width = "1px";
            d.style.paddingRight = "0";
            d.style.borderRight = "none";
            $WH.ae(d, Icon.create(f.icon, (this.iconSize == null ? 0 : this.iconSize), null, "javascript:;"));
            $WH.ae(e, d);
            g.style.borderLeft = "none";
            var b = $WH.ce("span");
            b.className = "listview-cleartext";
            $WH.ae(b, $WH.ct(f.name));
            $WH.ae(g, b);
            var c = this;
            $(e).click(function(a) {
               if (a.which != 2 || a.target != b) {
                  a.preventDefault();
                  c._calc.picker.chosenAbility.call(c._calc, f)
               }
            })
         },
         getVisibleText: function(a) {
            return a.name
         }
      }, {
         id: "description",
         name: LANG.description,
         type: "text",
         align: "left",
         compute: function(a, c) {
            var b = $WH.ce("div");
            b.className = "small";
            $WH.ae(b, $WH.ct(a.description));
            $WH.ae(c, b)
         },
         sortFunc: function(d, c) {
            return $WH.strcmp(d.description, c.description)
         }
      }, {
         id: "type",
         name: LANG.type,
         type: "text",
         compute: function(b, c) {
            var a = b.flags & 1;
            $WH.st(c, g_garrison_ability_types[b.trait ? 1 : 0])
         },
         getVisibleText: function(a) {
            return g_garrison_ability_types[a.trait ? 1 : 0]
         },
         sortFunc: function(d, c) {
            return $WH.strcmp(g_garrison_ability_types[d.trait ? 1 : 0], g_garrison_ability_types[c.trait ? 1 : 0])
         }
      }, {
         id: "category",
         name: LANG.category,
         type: "text",
         hidden: "true",
         compute: function(a, b) {
            if (g_garrison_ability_categories.hasOwnProperty(a.category)) {
               $WH.st(b, g_garrison_ability_categories[a.category])
            }
         },
         getVisibleText: function(a) {
            if (g_garrison_ability_categories.hasOwnProperty(a.category)) {
               return g_garrison_ability_categories[a.category]
            }
            return ""
         },
         sortFunc: function(d, c) {
            return $WH.strcmp(g_garrison_ability_categories.hasOwnProperty(d.category) ? g_garrison_ability_categories[d.category] : "", g_garrison_ability_categories.hasOwnProperty(c.category) ? g_garrison_ability_categories[c.category] : "")
         }
      }, {
         id: "counters",
         name: LANG.counters,
         type: "text",
         span: 2,
         align: "left",
         compute: function(e, c, h) {
            if (!e.hasOwnProperty("counters")) {
               return
            }
            var k = 0;
            var f = $WH.ce("td");
            f.style.width = "1px";
            f.style.paddingRight = "0";
            f.style.borderRight = "none";
            $WH.ae(h, f);
            c.style.borderLeft = "none";
            c.style.paddingLeft = "0";
            c.style.lineHeight = "26px";
            for (var j in e.counters) {
               if (!e.counters.hasOwnProperty(j)) {
                  continue
               }
               var b = e.counters[j];
               if (!b) {
                  continue
               }
               var a = g_garrison_mechanics[b];
               var g = $WH.ce("div");
               g.style.display = "inline-block";
               if (a.hasOwnProperty("icon")) {
                  $WH.ae(f, Icon.create(a.icon, 0, null, "javascript:;"))
               }
               var l = $WH.ce("span");
               l.style.whiteSpace = "nowrap";
               $WH.st(l, a.name);
               $WH.ae(g, l);
               if (a.hasOwnProperty("description") && a.description != "") {
                  $WH.Tooltip.simple(l, a.description);
                  l.className = "tip"
               }
               if (k++ > 0) {
                  $WH.ae(c, $WH.ce("br"))
               }
               $WH.ae(c, g)
            }
         },
         getVisibleText: function(b) {
            var a = "";
            for (var c in b.counters) {
               if (!b.counters.hasOwnProperty(c)) {
                  continue
               }
               if (!b.counters[c]) {
                  continue
               }
               a += g_garrison_mechanics[b.counters[c]].name + " "
            }
            return a
         },
         sortFunc: function(e, d) {
            var f = this.getVisibleText(e);
            var c = this.getVisibleText(d);
            return $WH.strcmp(f, c)
         }
      }]
   }
});
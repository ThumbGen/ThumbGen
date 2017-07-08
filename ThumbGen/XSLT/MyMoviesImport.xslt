<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
  <xsl:output method="xml" indent="yes"/>
  <xsl:template match="Title">
    <xsl:element name="movie">
      <xsl:element name="title">
        <xsl:value-of select="LocalTitle"/>
      </xsl:element>
      <xsl:element name="originaltitle">
        <xsl:value-of select="OriginalTitle"/>
      </xsl:element>
      <xsl:element name="year">
        <xsl:value-of select="ProductionYear"/>
      </xsl:element>
      <xsl:element name="plot">
        <xsl:value-of select="Description"/>
      </xsl:element>
      <xsl:element name="rating">
        <xsl:value-of select="Rating"/>
      </xsl:element>
      <xsl:element name="releasedate">
        <xsl:value-of select="ReleaseDate"/>
      </xsl:element>
      <xsl:element name="id">
        <xsl:value-of select="IMDB"/>
      </xsl:element>
      <xsl:element name="actor">
        <xsl:call-template name="actor"/>
      </xsl:element>
      <xsl:element name="genre">
        <xsl:call-template name="genre"/>
      </xsl:element>
      <xsl:element name="director">
        <xsl:element name="name">
          <xsl:value-of select="Persons/Person[@Type='2']/Name"/>
        </xsl:element>
      </xsl:element>
      <xsl:element name="runtime">
        <xsl:value-of select="RunningTime"/>
      </xsl:element>
      <xsl:element name="mpaa">
        <xsl:value-of select="ParentalRating/Description"/>
      </xsl:element>
      <xsl:element name="certification">
        <xsl:value-of select="certification"/>
      </xsl:element>
      <xsl:element name="studio">
        <xsl:call-template name="studio"/>
      </xsl:element>
      <xsl:element name="country">
        <xsl:element name="name">
          <xsl:value-of select="Country"/>
        </xsl:element>
      </xsl:element>
    </xsl:element>
  </xsl:template>
  <xsl:template name="actor">
    <xsl:for-each select="Persons/Person[@Type='1']">
      <xsl:element name="name">
        <xsl:value-of select="Name"/>
      </xsl:element>
    </xsl:for-each>
  </xsl:template>
  <xsl:template name="genre">
    <xsl:for-each select="Genres/Genre">
      <xsl:element name="name">
        <xsl:value-of select="."/>
      </xsl:element>
    </xsl:for-each>
  </xsl:template>
  <xsl:template name="studio">
    <xsl:for-each select="Studios/Studio">
      <xsl:element name="name">
        <xsl:value-of select="."/>
      </xsl:element>
    </xsl:for-each>
  </xsl:template>


  <xsl:template name="genre3">
    <!--<xsl:analyze-string select="genre" regex="(\b\S[^/]*)">
			<xsl:matching-substring>
				<xsl:element name="name">
					<xsl:value-of select="regex-group(1)"/>
				</xsl:element>
			</xsl:matching-substring>
		</xsl:analyze-string>-->
    <xsl:call-template name="divide">
      <xsl:with-param name="to-be-divided" select="genre"/>
      <xsl:with-param name="delimiter" select="'/'"/>
    </xsl:call-template>
  </xsl:template>
  <xsl:template name="divide">
    <xsl:param name="to-be-divided"/>
    <xsl:param name="delimiter"/>
    <xsl:choose>
      <xsl:when test="contains($to-be-divided,$delimiter)">
        <name>
          <xsl:value-of select="translate(substring-before($to-be-divided,$delimiter) ,' ', '') "/>
        </name>
        <xsl:call-template name="divide">
          <xsl:with-param name="to-be-divided" select="substring-after($to-be-divided,$delimiter)"/>
          <xsl:with-param name="delimiter" select="'/'"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <name>
          <xsl:value-of select="translate($to-be-divided, ' ', '' )"/>
        </name>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>


